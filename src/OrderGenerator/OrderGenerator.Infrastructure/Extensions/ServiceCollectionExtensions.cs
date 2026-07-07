using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderGenerator.Domain.Interfaces;
using OrderGenerator.Infrastructure.Configuration;
using OrderGenerator.Infrastructure.Persistence;

namespace OrderGenerator.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ExchangeSettings>? configureExchangeSettings = null)
    {
        if (configureExchangeSettings is not null)
            services.Configure(configureExchangeSettings);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=orderdb;Username=postgres;Password=postgres";

        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}