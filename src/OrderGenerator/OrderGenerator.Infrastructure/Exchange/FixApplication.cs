using OrderGenerator.Domain.Entities;
using OrderGenerator.Domain.Enums;
using QuickFix;
using System.Collections.Concurrent;

namespace OrderGenerator.Infrastructure.Exchange;

public class FixApplication : MessageCracker, IApplication
{
    public bool Connected => _session is not null;

    private Session? _session;
    private readonly ConcurrentDictionary<Guid, (Order Order, TaskCompletionSource<Order> Tcs)> _pendingOrders = new();

    public FixApplication()
    {
        _session = null;
    }

    public async Task<Order> SendOrder(Order order)
    {
        Console.WriteLine($"[SendOrder] Sending order {order.Id}");

        if (_session == null)
            throw new InvalidOperationException("FIX session is not established.");

        var tcs = new TaskCompletionSource<Order>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingOrders[order.Id] = (order, tcs);

        var newOrderSingle = new QuickFix.FIX44.NewOrderSingle(
            new QuickFix.Fields.ClOrdID(order.Id.ToString()),
            new QuickFix.Fields.Symbol(order.Ticker),
            new QuickFix.Fields.Side(order.Side == OrderSide.BUY ? QuickFix.Fields.Side.BUY : QuickFix.Fields.Side.SELL),
            new QuickFix.Fields.TransactTime(DateTime.UtcNow),
            new QuickFix.Fields.OrdType(QuickFix.Fields.OrdType.LIMIT));

        newOrderSingle.Set(new QuickFix.Fields.OrderQty(order.Quantity));
        newOrderSingle.Set(new QuickFix.Fields.Price(order.Price));

        _session.Send(newOrderSingle);

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _pendingOrders.TryRemove(order.Id, out _);
            order.Reject("Exchange response timeout");
            return order;
        }

        return await tcs.Task;
    }

    public void OnCreate(SessionID sessionId)
    {
        Console.WriteLine($"[OnCreate] {sessionId}");
        _session = Session.LookupSession(sessionId);
    }

    public void OnLogon(SessionID sessionId)
                    => Console.WriteLine($"[OnLogon] {sessionId}");

    public void OnLogout(SessionID sessionId)
                    => Console.WriteLine($"[OnLogout] {sessionId}");

    public void FromAdmin(QuickFix.Message message, SessionID sessionId)
                    => PrintMessage("FROM ADMIN", message);

    public void ToAdmin(QuickFix.Message message, SessionID sessionId)
                    => PrintMessage("TO ADMIN", message);

    public void FromApp(QuickFix.Message message, SessionID sessionId)
                    => Crack(message, sessionId);

    public void ToApp(QuickFix.Message message, SessionID sessionId)
                    => PrintMessage("TO APP", message);
    public static void PrintMessage(string direction, QuickFix.Message fixMessage)
    {
        Console.WriteLine($"[{direction}]");

        var message = fixMessage.ToString();
        var fields = message.Split('', StringSplitOptions.RemoveEmptyEntries);

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

            Console.WriteLine($"{tag,-3} {name,-20} = {displayValue}");
        }
        Console.WriteLine("");
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
        [103] = new()
        {
            ["0"] = "Broker option",
            ["1"] = "Unknown symbol",
            ["2"] = "Exchange closed",
            ["3"] = "Order exceeds limit",
            ["4"] = "Too late to enter",
            ["5"] = "Unknown order",
            ["6"] = "Duplicate order",
            ["99"] = "Other"
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
                                            { 60, "TransactTime" },
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
    public void OnMessage(
        QuickFix.FIX44.ExecutionReport report,
        SessionID sessionId)
    {
        PrintMessage("EXECUTION REPORT", report);

        string clOrdIdStr = report.ClOrdID.Value;
        if (Guid.TryParse(clOrdIdStr, out Guid orderId))
        {
            if (_pendingOrders.TryRemove(orderId, out var pending))
            {
                var order = pending.Order;
                var tcs = pending.Tcs;

                var status = report.OrdStatus.Value;
                if (status == QuickFix.Fields.OrdStatus.REJECTED)
                {
                    string reason = report.Text.Value.ToString();
                    order.Reject(reason);
                }
                else if (status == QuickFix.Fields.OrdStatus.NEW || status == QuickFix.Fields.OrdStatus.FILLED)
                {
                    order.Accept(report.OrderID.Value);
                }
                else
                {
                    order.Accept(report.OrderID.Value);
                }

                tcs.TrySetResult(order);
            }
        }
    }
}
