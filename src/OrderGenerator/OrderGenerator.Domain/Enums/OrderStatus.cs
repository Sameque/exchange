namespace OrderGenerator.Domain.Enums;

/// <summary>
/// Lifecycle status of an order.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Filled = 3,
    Cancelled = 4
}
