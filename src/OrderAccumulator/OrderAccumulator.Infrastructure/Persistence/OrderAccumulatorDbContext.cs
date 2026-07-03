using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Entities;

namespace OrderAccumulator.Infrastructure.Persistence;

public class OrderAccumulatorDbContext : DbContext
{
    public OrderAccumulatorDbContext(DbContextOptions<OrderAccumulatorDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Symbol> Symbols { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Symbol>().HasKey(s => s.Ticker);
    }
}
