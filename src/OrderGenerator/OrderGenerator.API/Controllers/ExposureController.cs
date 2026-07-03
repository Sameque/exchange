using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Application.DTOs;
using OrderGenerator.Application.UseCases;

namespace OrderGenerator.API.Controllers;

/// <summary>
/// Provides financial exposure data per stock symbol.
/// </summary>
[ApiController]
[Route("api/exposed")]
[Produces("application/json")]
public sealed class ExposureController : ControllerBase
{
    private readonly GetExposureUseCase _getExposureUseCase;

    public ExposureController(GetExposureUseCase getExposureUseCase)
    {
        _getExposureUseCase = getExposureUseCase ??
            throw new ArgumentNullException(nameof(getExposureUseCase));
    }

    /// <summary>
    /// Returns the total financial exposure (price × quantity) per symbol
    /// for all accepted orders.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of symbols with their total exposed value.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ExposureResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExposure(CancellationToken cancellationToken)
    {
        var exposure = await _getExposureUseCase.ExecuteAsync(cancellationToken);
        return Ok(exposure);
    }
}
