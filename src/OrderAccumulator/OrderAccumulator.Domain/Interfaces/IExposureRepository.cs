namespace OrderAccumulator.Domain.Interfaces;

public interface IExposureRepository
{
    Task<decimal> GetExposureAsync(string symbol);
    Task UpdateExposureAsync(string symbol, decimal delta);
}
