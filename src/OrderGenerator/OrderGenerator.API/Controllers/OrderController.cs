using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Application.DTOs;
using OrderGenerator.Application.UseCases;

namespace OrderGenerator.API.Controllers;

/// <summary>
/// Handles order submission to the FIX exchange.
/// </summary>
[ApiController]
[Route("api/order")]
[Produces("application/json")]
public sealed class OrderController : ControllerBase
{
    private readonly PlaceOrderUseCase _placeOrderUseCase;
    private readonly ILogger<OrderController> _logger;

    public OrderController(PlaceOrderUseCase placeOrderUseCase, ILogger<OrderController> logger)
    {
        _placeOrderUseCase = placeOrderUseCase ?? 
                        throw new ArgumentNullException(nameof(placeOrderUseCase));
        
        _logger = logger ?? 
                        throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submits a new buy or sell order to the exchange via FIX 4.4.
    /// </summary>
    /// <param name="request">Order details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order confirmation from the exchange.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PlaceOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received order request. Symbol={Symbol} Side={Side} Qty={Qty} Price={Price}",
            request.Symbol, request.Side, request.Quantity, request.Price);

        var response = await _placeOrderUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(PlaceOrder), new { id = response.OrderId }, response);
    }
}
