using QuickFix;
using QuickFix.FIX44;
using OrderAccumulator.Application.DTOs;
using OrderAccumulator.Application.UseCases;
using QuickFix.Fields;

namespace OrderAccumulator.API.Fix;

public class FixAcceptor : MessageCracker, IApplication
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FixAcceptor> _logger;

    public FixAcceptor(IServiceProvider serviceProvider, ILogger<FixAcceptor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void FromApp(QuickFix.Message message, SessionID sessionID)
    {
        try
        {
            if (message is NewOrderSingle orderMsg)
            {
                _logger.LogInformation("FIX Order received: {Symbol}, {Qty}, {Price}",
                    orderMsg.Symbol.Value, orderMsg.OrderQty.Value, orderMsg.Price.Value);

                using var scope = _serviceProvider.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ProcessOrderUseCase>();

                var request = new OrderRequest(
                    Symbol: orderMsg.Symbol.Value,
                    Quantity: orderMsg.OrderQty.Value,
                    Price: orderMsg.Price.Value,
                    Side: orderMsg.Side.Value == Side.BUY ? "buy" : "sell"
                );

                var response = Task.Run(() => useCase.ExecuteAsync(request)).GetAwaiter().GetResult();

                SendExecutionReport(sessionID, orderMsg, response.Accepted, response.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FIX order");
            if (message is NewOrderSingle orderMsg)
                SendExecutionReport(sessionID, orderMsg, false, $"Internal error: {ex.Message}");
        }
    }

    private void SendExecutionReport(
                    SessionID sessionID,
                    NewOrderSingle orderMsg,
                    bool accepted,
                    string? reason = null)
    {
        var report = new ExecutionReport(
                            new OrderID(Guid.NewGuid().ToString("N")),
                            new ExecID(Guid.NewGuid().ToString("N")),
                            new ExecType(accepted ? ExecType.NEW : ExecType.REJECTED),
                            new OrdStatus(accepted ? OrdStatus.NEW : OrdStatus.REJECTED),
                            orderMsg.Symbol,
                            orderMsg.Side,
                            new LeavesQty(accepted ? 0 : orderMsg.OrderQty.Value),
                            new CumQty(accepted ? orderMsg.OrderQty.Value : 0),
                            new AvgPx());

        report.SetField(orderMsg.ClOrdID);
        report.SetField(orderMsg.OrderQty);
        report.SetField(orderMsg.Price);

        if (!accepted)
        {
            report.SetField(new OrdRejReason(OrdRejReason.BROKER_OPTION));
            report.SetField(new Text(reason ?? "Order rejected by the system."));
        }

        Session.SendToTarget(report, sessionID);
    }

    public void OnCreate(SessionID sessionID)
                    => _logger.LogInformation("[OnCreate] {SessionId}", sessionID);

    public void OnLogon(SessionID sessionId)
                    => _logger.LogInformation("[OnLogon] {SessionId}", sessionId);

    public void OnLogout(SessionID sessionId)
                    => _logger.LogInformation("[OnLogout] {SessionId}", sessionId);

    public void FromAdmin(QuickFix.Message message, SessionID sessionId)
                    => PrintMessage("FROM ADMIN", message);

    public void ToAdmin(QuickFix.Message message, SessionID sessionId)
                    => PrintMessage("TO ADMIN", message);

    public void ToApp(QuickFix.Message message, SessionID sessionId)
                    => PrintMessage("TO APP", message);

    private void PrintMessage(string direction, QuickFix.Message fixMessage)
    {
        _logger.LogInformation("[{Direction}]", direction);

        var message = fixMessage.ToString();
        var fields = message.Split('\u0001', StringSplitOptions.RemoveEmptyEntries);

        foreach (var field in fields)
        {
            var parts = field.Split('=', 2);

            if (parts.Length != 2)
                continue;

            int tag = int.Parse(parts[0]);
            string value = parts[1];

            string name = FixFields.TryGetValue(tag, out var fieldName)
                ? fieldName
                : "Unknown";

            string displayValue = value;

            if (FixEnums.TryGetValue(tag, out var values) &&
                values.TryGetValue(value, out var description))
            {
                displayValue = $"{value} ({description})";
            }

            _logger.LogInformation("{Tag,-3} {Name,-20} = {Value}", tag, name, displayValue);
        }
    }

    private static readonly Dictionary<int, Dictionary<string, string>> FixEnums = new()
    {
        [54] = new()
        {
            ["1"] = "Buy",
            ["2"] = "Sell"
        },
        [39] = new()
        {
            ["0"] = "New",
            ["1"] = "Partially Filled",
            ["2"] = "Filled",
            ["4"] = "Canceled",
            ["8"] = "Rejected"
        },
        [123] = new()
        {
            ["Y"] = "Yes",
            ["N"] = "No"
        },
        [150] = new()
        {
            ["0"] = "New",
            ["1"] = "Partial Fill",
            ["2"] = "Fill",
            ["4"] = "Canceled",
            ["8"] = "Rejected"
        }
    };

    private static readonly Dictionary<int, string> FixFields = new()
                                        {
                                            { 6, "AvgPx" },
                                            { 7,   "BeginSeqNo" },
                                            { 8, "BeginString" },
                                            { 9, "BodyLength" },
                                            { 10,  "CheckSum" },
                                            { 11, "ClOrdID" },
                                            { 14, "CumQty" },
                                            { 16,  "EndSeqNo" },
                                            { 17, "ExecID" },
                                            { 34, "MsgSeqNum" },
                                            { 35, "MsgType" },
                                            { 36,  "NewSeqNo" },
                                            { 37, "OrderID" },
                                            { 38, "OrderQty" },
                                            { 39, "OrdStatus" },
                                            { 44, "Price" },
                                            { 49, "SenderCompID" },
                                            { 52, "SendingTime" },
                                            { 54, "Side" },
                                            { 55, "Symbol" },
                                            { 56, "TargetCompID" },
                                            { 58,  "Text" },
                                            { 98, "EncryptMethod" },
                                            { 103, "OrdRejReason" },
                                            { 108, "HeartBtInt" },
                                            { 112, "TestReqID" },
                                            { 122, "OrigSendingTime" },
                                            { 123, "GapFillFlag" },
                                            { 150, "ExecType" },
                                            { 151, "LeavesQty" },
                                        };
}
