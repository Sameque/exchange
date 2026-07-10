using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal Price { get; private set; }
    public OrderSide Side { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime Timestamp { get; private set; }

    private Order() { }

    public static Order Create(string symbol, decimal quantity, decimal price, OrderSide side)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = symbol.ToUpper(),
            Quantity = quantity,
            Price = price,
            Side = side,
            Status = OrderStatus.Accepted,
            Timestamp = DateTime.UtcNow
        };
    }

    public void Reject()
    {
        Status = OrderStatus.Rejected;
    }
}
