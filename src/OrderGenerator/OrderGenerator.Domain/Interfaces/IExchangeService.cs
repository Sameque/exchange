using OrderGenerator.Domain.Entities;

namespace OrderGenerator.Domain.Interfaces;

/// <summary>
/// Contract for communicating with the FIX exchange.
/// Implementations depend on QuickFIXn and live in Infrastructure.
/// </summary>
public interface IExchangeService
{
    /// <summary>
    /// Sends a new order to the FIX exchange and returns the updated order
    /// (accepted or rejected) after the exchange response is received.
    /// </summary>
    Task<Order> SendOrderAsync(Order order, CancellationToken cancellationToken = default);
}
