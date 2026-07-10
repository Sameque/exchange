using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Entities;

namespace OrderAccumulator.Infrastructure.Persistence;

public class OrderAccumulatorDbContext : DbContext
{
    public OrderAccumulatorDbContext(DbContextOptions<OrderAccumulatorDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Price).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.HasIndex(e => e.Symbol);
        });
    }
}
