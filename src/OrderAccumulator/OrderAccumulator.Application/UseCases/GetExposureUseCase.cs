using OrderAccumulator.Application.DTOs;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Domain.Interfaces;

namespace OrderAccumulator.Application.UseCases;

public class GetExposureUseCase
{
    private readonly IOrderRepository _repository;

    public GetExposureUseCase(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ExposureResponse>> ExecuteAsync()
    {
        var allSymbols = await _repository.GetSymbolsAsync();
        var exposures = new List<ExposureResponse>();

        foreach (var symbol in allSymbols)
        {
            var orders = await _repository.GetOrdersBySymbolAsync(symbol.Ticker);

            // Exposure = Sum(Buy * Price) - Sum(Sell * Price)
            var exposure = orders.Sum(o =>
                o.Side == OrderSide.Buy
                    ? o.Quantity * o.Price
                    : -o.Quantity * o.Price);

            exposures.Add(new ExposureResponse(symbol.Ticker, exposure));
        }

        return exposures;
    }
}
