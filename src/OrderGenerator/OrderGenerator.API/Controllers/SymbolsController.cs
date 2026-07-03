using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Application.DTOs;
using OrderGenerator.Application.UseCases;

namespace OrderGenerator.API.Controllers;

/// <summary>
/// Provides the list of available tradeable stock symbols.
/// </summary>
[ApiController]
[Route("api/symbols")]
[Produces("application/json")]
public sealed class SymbolsController : ControllerBase
{
    private readonly GetSymbolsUseCase _getSymbolsUseCase;

    public SymbolsController(GetSymbolsUseCase getSymbolsUseCase)
    {
        _getSymbolsUseCase = getSymbolsUseCase ??
            throw new ArgumentNullException(nameof(getSymbolsUseCase));
    }

    /// <summary>
    /// Returns all available stock symbols for trading.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of symbols with their descriptions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SymbolResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSymbols(CancellationToken cancellationToken)
    {
        var symbols = await _getSymbolsUseCase.ExecuteAsync(cancellationToken);
        return Ok(symbols);
    }
}
