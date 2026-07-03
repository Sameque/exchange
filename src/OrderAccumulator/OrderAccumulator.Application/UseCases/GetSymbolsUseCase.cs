using OrderAccumulator.Application.DTOs;
using OrderAccumulator.Domain.Interfaces;

namespace OrderAccumulator.Application.UseCases;

public class GetSymbolsUseCase
{
    private readonly IOrderRepository _repository;

    public GetSymbolsUseCase(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SymbolResponse>> ExecuteAsync()
    {
        var symbols = await _repository.GetSymbolsAsync();
        return symbols.Select(s => new SymbolResponse(s.Ticker, s.Description));
    }
}
