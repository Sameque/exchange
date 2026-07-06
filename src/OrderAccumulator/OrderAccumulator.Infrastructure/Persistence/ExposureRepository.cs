using Microsoft.Extensions.Caching.Distributed;
using OrderAccumulator.Domain.Interfaces;

namespace OrderAccumulator.Infrastructure.Persistence;

public class ExposureRepository : IExposureRepository
{
    private readonly IDistributedCache _cache;
    private const string CacheKeyPrefix = "exposure_";

    public ExposureRepository(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<decimal> GetExposureAsync(string symbol)
    {
        string key = $"{CacheKeyPrefix}{symbol.ToUpper()}";
        var value = await _cache.GetStringAsync(key);

        if (decimal.TryParse(value, out decimal exposure))
            return exposure;

        return 0m;
    }

    public async Task UpdateExposureAsync(string symbol, decimal delta)
    {
        string key = $"{CacheKeyPrefix}{symbol.ToUpper()}";

        decimal currentExposure = await GetExposureAsync(symbol);
        decimal newValue = currentExposure + delta;

        await _cache.SetStringAsync(key, newValue.ToString());
    }
}
