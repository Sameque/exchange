using OrderAccumulator.Application.DTOs;
using OrderAccumulator.Domain.Entities;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Domain.Interfaces;

namespace OrderAccumulator.Application.UseCases;

public class ProcessOrderUseCase
{
    private readonly IOrderRepository _repository;

    public ProcessOrderUseCase(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderResponse> ExecuteAsync(OrderRequest request)
    {
        // Simple validation
        if (string.IsNullOrWhiteSpace(request.Symbol) || request.Quantity <= 0 || request.Price <= 0)
        {
            return new OrderResponse(false, "Invalid order parameters.");
        }

        var side = request.Side.ToLower() == "buy" ? OrderSide.Buy : OrderSide.Sell;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = request.Symbol.ToUpper(),
            Quantity = request.Quantity,
            Price = request.Price,
            Side = side,
            Status = OrderStatus.Accepted,
            Timestamp = DateTime.UtcNow
        };

        await _repository.AddOrderAsync(order);

        return new OrderResponse(true, "Order accepted and processed.");
    }
}
