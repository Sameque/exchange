using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderGenerator.Application.DTOs;
using OrderGenerator.Domain.Interfaces;
using OrderGenerator.Infrastructure.Configuration;

namespace OrderGenerator.Infrastructure.Exchange;

/// <summary>
/// Implementation of IExchangeApiClient that communicates with the Exchange via REST API.
/// </summary>
public sealed class ExchangeApiClient : IExchangeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeApiClient> _logger;

    public ExchangeApiClient(HttpClient httpClient, ILogger<ExchangeApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SymbolExistsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/symbols/{symbol.ToUpperInvariant()}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking symbol existence for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<IReadOnlyList<SymbolResponse>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var symbols = await _httpClient.GetFromJsonAsync<List<SymbolResponse>>("/symbols", cancellationToken);
            return symbols?.AsReadOnly() ?? new List<SymbolResponse>().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving symbols from exchange");
            throw;
        }
    }

    public async Task<IReadOnlyList<ExposureResponse>> GetExposureAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exposure = await _httpClient.GetFromJsonAsync<List<ExposureResponse>>("/exposure", cancellationToken);
            return exposure?.AsReadOnly() ?? new List<ExposureResponse>().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exposure from exchange");
            throw;
        }
    }
}
