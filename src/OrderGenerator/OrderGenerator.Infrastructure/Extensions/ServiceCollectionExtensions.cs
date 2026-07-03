using Microsoft.Extensions.DependencyInjection;
using OrderGenerator.Domain.Interfaces;
using OrderGenerator.Infrastructure.Exchange;
using OrderGenerator.Infrastructure.Configuration;

namespace OrderGenerator.Infrastructure.Extensions;

/// <summary>
/// Extension method to register all infrastructure services
/// following the Dependency Inversion principle.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<ExchangeSettings>? configureExchangeSettings = null)
    {
        if (configureExchangeSettings is not null)
            services.Configure(configureExchangeSettings);

        services.AddSingleton<IExchangeService, FixExchangeService>();

        services.AddHttpClient<IExchangeApiClient, ExchangeApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<ExchangeSettings>>().Value;
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
        });

        return services;
    }
}
