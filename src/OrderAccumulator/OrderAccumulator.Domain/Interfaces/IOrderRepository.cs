using OrderAccumulator.Domain.Entities;

namespace OrderAccumulator.Domain.Interfaces;

public interface IOrderRepository
{
    Task AddOrderAsync(Order order);
    Task<IEnumerable<Order>> GetOrdersBySymbolAsync(string symbol);
}
