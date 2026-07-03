using OrderGenerator.Application.DTOs;

namespace OrderGenerator.Domain.Interfaces;

/// <summary>
/// REST API Client for interacting with the Exchange's information services.
/// </summary>
public interface IExchangeApiClient
{
    /// <summary>
    /// Checks if a stock symbol is available for trading via the Exchange API.
    /// </summary>
    Task<bool> SymbolExistsAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tradeable symbols from the Exchange API.
    /// </summary>
    Task<IReadOnlyList<SymbolResponse>> GetSymbolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current financial exposure from the Exchange API.
    /// </summary>
    Task<IReadOnlyList<ExposureResponse>> GetExposureAsync(CancellationToken cancellationToken = default);
}
