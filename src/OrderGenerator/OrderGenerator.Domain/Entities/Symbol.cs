namespace OrderGenerator.Domain.Entities;

/// <summary>
/// Represents a tradeable stock symbol listed on the exchange.
/// </summary>
public class Symbol
{
    public string Ticker { get; private set; }
    public string? Description { get; private set; }

    private Symbol() { Ticker = string.Empty; }

    public Symbol(string ticker, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        Ticker = ticker.ToUpperInvariant();
        Description = description;
    }
}
