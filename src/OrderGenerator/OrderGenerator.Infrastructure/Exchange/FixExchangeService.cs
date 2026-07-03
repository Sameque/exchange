using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderGenerator.Domain.Entities;
using OrderGenerator.Domain.Enums;
using OrderGenerator.Domain.Interfaces;
using OrderGenerator.Infrastructure.Configuration;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace OrderGenerator.Infrastructure.Exchange;

/// <summary>
/// FIX 4.4 exchange implementation using QuickFIXn 1.14.1.
/// Sends NewOrderSingle (FIX tag 35=D) and awaits ExecutionReport (35=8).
/// When the exchange is unreachable the service falls back to simulation mode
/// so the REST API remains functional during development.
/// </summary>
public sealed class FixExchangeService : IExchangeService, IApplication, IDisposable
{
    private readonly FixSettings _settings;
    private readonly ILogger<FixExchangeService> _logger;

    private SocketInitiator? _initiator;
    private Session? _session;

    private readonly Dictionary<string, TaskCompletionSource<QuickFix.FIX44.ExecutionReport>> _pendingOrders =
        new(StringComparer.Ordinal);

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isConnected;

    public FixExchangeService(IOptions<FixSettings> settings, ILogger<FixExchangeService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitiateSession();
    }

    private void InitiateSession()
    {
        try
        {
            var settingsText = BuildFixSettingsContent();
            using var reader = new StringReader(settingsText);

            var sessionSettings = new SessionSettings(reader);
            var storeFactory = new MemoryStoreFactory();
            var logFactory = new NullQuickFixLogFactory(sessionSettings);

            _initiator = new SocketInitiator(this, storeFactory, sessionSettings, logFactory);
            _initiator.Start();

            _logger.LogInformation("FIX initiator started. Connecting to {Host}:{Port}",
                _settings.Host, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not start FIX initiator. Service will run in simulation mode.");
        }
    }

    private string BuildFixSettingsContent() =>
        $"""
        [DEFAULT]
        ConnectionType=initiator
        HeartBtInt=30
        SenderCompID={_settings.SenderCompId}
        TargetCompID={_settings.TargetCompId}
        StartTime=00:00:00
        EndTime=00:00:00
        UseDataDictionary=N

        [SESSION]
        BeginString=FIX.4.4
        SocketConnectHost={_settings.Host}
        SocketConnectPort={_settings.Port}
        """;

    public void OnCreate(SessionID sessionID)
    {
        _session = Session.LookupSession(sessionID);
        _logger.LogInformation("FIX session created: {SessionID}", sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        _isConnected = true;
        _logger.LogInformation("FIX session logged on: {SessionID}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _isConnected = false;
        _logger.LogWarning("FIX session logged out: {SessionID}", sessionID);
    }

    public void ToAdmin(QuickFix.Message message, SessionID sessionID) { }

    public void FromAdmin(QuickFix.Message message, SessionID sessionID) { }

    public void ToApp(QuickFix.Message message, SessionID sessionID) { }

    public void FromApp(QuickFix.Message message, SessionID sessionID)
    {
        var msgType = message.Header.GetString(Tags.MsgType);

        if (msgType == MsgType.EXECUTION_REPORT)
        {
            HandleExecutionReport((QuickFix.FIX44.ExecutionReport)message);
        }
    }

    private void HandleExecutionReport(QuickFix.FIX44.ExecutionReport report)
    {
        try
        {
            var clOrdId = report.ClOrdID.Value;

            _semaphore.Wait();
            try
            {
                if (_pendingOrders.TryGetValue(clOrdId, out var tcs))
                {
                    _pendingOrders.Remove(clOrdId);
                    tcs.TrySetResult(report);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            _logger.LogInformation(
                "ExecutionReport received. ClOrdID={ClOrdID} ExecType={ExecType}",
                clOrdId, report.ExecType.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ExecutionReport.");
        }
    }

    public async Task<Order> SendOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var clOrdId = order.Id.ToString("N");

        if (!_isConnected || _session is null)
        {
            _logger.LogWarning(
                "FIX not connected — simulating exchange acceptance for order {OrderId}.", order.Id);
            order.Accept(exchangeOrderId: $"SIM-{clOrdId[..8].ToUpperInvariant()}");
            return order;
        }

        var tcs = new TaskCompletionSource<QuickFix.FIX44.ExecutionReport>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _pendingOrders[clOrdId] = tcs;
        }
        finally
        {
            _semaphore.Release();
        }

        try
        {
            var fixOrder = BuildNewOrderSingle(order, clOrdId);
            Session.SendToTarget(fixOrder, _session.SessionID);

            _logger.LogInformation(
                "NewOrderSingle sent. Symbol={Symbol} Side={Side} Qty={Qty} Price={Price} ClOrdID={ClOrdID}",
                order.Symbol, order.Side, order.Quantity, order.Price, clOrdId);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_settings.ResponseTimeoutSeconds));

            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                var report = await tcs.Task;
                return ApplyExecutionReport(order, report);
            }
        }
        catch (OperationCanceledException)
        {
            await _semaphore.WaitAsync(CancellationToken.None);
            try { _pendingOrders.Remove(clOrdId); }
            finally { _semaphore.Release(); }

            order.Reject("Exchange response timed out.");
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order {OrderId} to FIX exchange.", order.Id);
            order.Reject(ex.Message);
            return order;
        }
    }

    private static QuickFix.FIX44.NewOrderSingle BuildNewOrderSingle(Order order, string clOrdId)
    {
        var fixOrder = new QuickFix.FIX44.NewOrderSingle(
            new ClOrdID(clOrdId),
            new QuickFix.Fields.Symbol(order.Symbol),
            new Side(order.Side == OrderSide.BUY ? Side.BUY : Side.SELL),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT)
        )
        {
            OrderQty = new OrderQty(order.Quantity),
            Price = new QuickFix.Fields.Price(order.Price),
            TimeInForce = new TimeInForce(TimeInForce.DAY)
        };

        return fixOrder;
    }

    private static Order ApplyExecutionReport(Order order, QuickFix.FIX44.ExecutionReport report)
    {
        var ordStatus = report.OrdStatus.Value;

        if (ordStatus == OrdStatus.REJECTED)
        {
            var reason = report.IsSetText() ? report.Text.Value : "Rejected by exchange.";
            order.Reject(reason);
        }
        else
        {
            var exchangeOrderId = report.IsSetOrderID() ? report.OrderID.Value : report.ClOrdID.Value;
            order.Accept(exchangeOrderId);
        }

        return order;
    }

    public void Dispose()
    {
        _initiator?.Stop();
        _initiator?.Dispose();
        _semaphore.Dispose();
    }
}

/// <summary>
/// Minimal ILogFactory that suppresses all FIX protocol-level logs.
/// Use the application-level ILogger for relevant diagnostic output instead.
/// </summary>
file sealed class NullQuickFixLogFactory : QuickFix.Logger.ILogFactory
{
    public NullQuickFixLogFactory(SessionSettings settings) { }

    public QuickFix.Logger.ILog Create(SessionID sessionID) => new NullQuickFixLog();

    public QuickFix.Logger.ILog CreateNonSessionLog() => new NullQuickFixLog();
}

file sealed class NullQuickFixLog : QuickFix.Logger.ILog
{
    public void Clear() { }
    public void OnEvent(string s) { }
    public void OnIncoming(string s) { }
    public void OnOutgoing(string s) { }
    public void Dispose() { }
}
