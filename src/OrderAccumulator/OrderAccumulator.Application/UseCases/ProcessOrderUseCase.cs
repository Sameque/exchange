using OrderAccumulator.Application.DTOs;
using OrderAccumulator.Domain.Entities;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Domain.Interfaces;

namespace OrderAccumulator.Application.UseCases;

public class ProcessOrderUseCase
{
    private readonly IOrderRepository _repository;
    private readonly IExposureRepository _exposureRepository;

    public ProcessOrderUseCase(IOrderRepository repository, IExposureRepository exposureRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _exposureRepository = exposureRepository ?? throw new ArgumentNullException(nameof(exposureRepository));
    }

    public async Task<OrderResponse> ExecuteAsync(OrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol) || request.Quantity <= 0 || request.Price <= 0)
            return new OrderResponse(false, "Invalid order parameters.");

        if (request.Price > 1000)
            return new OrderResponse(false, $"Price exceeds the maximum allowed value of 1000.");

        if (request.Quantity > 100000)
            return new OrderResponse(false, $"Price exceeds the maximum allowed value of 1000.");

        var side = request.Side.ToLower() == "buy" ? OrderSide.Buy : OrderSide.Sell;

        var symbol = request.Symbol.ToUpper();
        var orderValue = request.Price * request.Quantity;
        var delta = side == OrderSide.Buy ? orderValue : -orderValue;
        var currentExposure = await _exposureRepository.GetExposureAsync(symbol);

        if (currentExposure + delta > 100000000.00m)
        {
            return new OrderResponse(false, $"Order rejected: Total exposure for {symbol} would exceed the maximum allowed limit of 100,000,000.00.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Quantity = request.Quantity,
            Price = request.Price,
            Side = side,
            Status = OrderStatus.Accepted,
            Timestamp = DateTime.UtcNow
        };

        await _repository.AddOrderAsync(order);
        await _exposureRepository.UpdateExposureAsync(symbol, delta);

        return new OrderResponse(true, "Order accepted and processed.");
    }
}
