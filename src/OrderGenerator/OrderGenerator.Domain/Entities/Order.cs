using OrderGenerator.Domain.Enums;

namespace OrderGenerator.Domain.Entities;

/// <summary>
/// Represents a stock purchase or sale order.
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public string Ticker { get; private set; }
    public OrderSide Side { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? ExchangeOrderId { get; private set; }
    public string? RejectReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Order() { Ticker = string.Empty; }

    public static Order Create(string symbol, OrderSide side, int quantity, decimal price)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Ticker = symbol.ToUpperInvariant(),
            Side = side,
            Quantity = quantity,
            Price = price,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Accept(string exchangeOrderId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeOrderId);
        ExchangeOrderId = exchangeOrderId;
        Status = OrderStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        RejectReason = reason;
        Status = OrderStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Total financial exposure of this order (price × quantity).
    /// </summary>
    public decimal NotionalValue => Price * Quantity;
}
