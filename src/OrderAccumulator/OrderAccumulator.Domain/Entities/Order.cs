using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public OrderSide Side { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
}
