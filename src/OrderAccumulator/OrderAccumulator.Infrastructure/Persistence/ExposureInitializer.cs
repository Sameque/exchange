using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Domain.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace OrderAccumulator.Infrastructure.Persistence;

public class ExposureInitializer
{
    private readonly OrderAccumulatorDbContext _db;
    private readonly IExposureRepository _exposureRepository;

    public ExposureInitializer(OrderAccumulatorDbContext db, IExposureRepository exposureRepository)
    {
        _db = db;
        _exposureRepository = exposureRepository;
    }

    public async Task InitializeAsync()
    {
        _db.Database.EnsureCreated();

        var orders = await _db.Orders.ToListAsync();
        var exposures = orders.GroupBy(o => o.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                TotalExposure = g.Sum(o => o.Side == OrderSide.Buy
                    ? o.Price * o.Quantity
                    : -o.Price * o.Quantity)
            });

        foreach (var exp in exposures)
        {
            await _exposureRepository.UpdateExposureAsync(exp.Symbol, exp.TotalExposure);
        }
    }
}
