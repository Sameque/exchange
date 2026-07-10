using OrderAccumulator.Domain.Entities;

namespace OrderAccumulator.Domain.Interfaces;

public interface IOrderRepository
{
    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetOrdersBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
}
