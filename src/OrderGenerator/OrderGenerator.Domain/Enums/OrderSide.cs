namespace OrderGenerator.Domain.Enums;

/// <summary>
/// Indicates whether the order is a buy or sell.
/// Maps to FIX 4.4 tag 54 (Side).
/// </summary>
public enum OrderSide
{
    BUY = 1,
    SELL = 2
}
