using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.UseCases;
using OrderAccumulator.Domain.Interfaces;
using OrderAccumulator.Infrastructure.Persistence;
using OrderAccumulator.API.Fix;
using OrderAccumulator.API.Observability;
using QuickFix;
using QuickFix.Store;
using QuickFix.Logger;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetry();

builder.Services.AddDbContext<OrderAccumulatorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddSingleton<IExposureRepository, ExposureRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ProcessOrderUseCase>();
builder.Services.AddScoped<ExposureInitializer>();

builder.Services.AddSingleton<FixAcceptor>();

var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<ExposureInitializer>();
        await initializer.InitializeAsync();
    }

var fixAcceptor = app.Services.GetRequiredService<FixAcceptor>();
var settings = new SessionSettings("config/client.cfg");
var storeFactory = new FileStoreFactory(settings);
var logFactory = new ScreenLogFactory(settings);

var initiator = new ThreadedSocketAcceptor(
                        fixAcceptor, 
                        storeFactory, 
                        settings, 
                        logFactory);

initiator.Start();

app.MapPrometheusScrapingEndpoint();

app.Run();
