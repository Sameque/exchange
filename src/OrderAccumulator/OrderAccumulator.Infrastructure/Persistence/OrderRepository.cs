using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Entities;
using OrderAccumulator.Domain.Interfaces;

namespace OrderAccumulator.Infrastructure.Persistence;

public class OrderRepository : IOrderRepository
{
    private readonly OrderAccumulatorDbContext _context;

    public OrderRepository(OrderAccumulatorDbContext context)
    {
        _context = context;
    }

    public async Task AddOrderAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersBySymbolAsync(string symbol)
    {
        return await _context.Orders
            .Where(o => o.Symbol == symbol)
            .ToListAsync();
    }

    public async Task<IEnumerable<Symbol>> GetSymbolsAsync()
    {
        return await _context.Symbols.ToListAsync();
    }
}
