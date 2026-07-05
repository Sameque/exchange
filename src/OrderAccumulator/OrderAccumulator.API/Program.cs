using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.UseCases;
using OrderAccumulator.Domain.Interfaces;
using OrderAccumulator.Infrastructure.Persistence;
using OrderAccumulator.API.Fix;
using QuickFix;
using QuickFix.Store;
using QuickFix.Logger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderAccumulatorDbContext>(options =>
    options.UseSqlite("Data Source=orders.db"));

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IExposureRepository, ExposureRepository>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<ProcessOrderUseCase>();

builder.Services.AddSingleton<FixAcceptor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderAccumulatorDbContext>();
    db.Database.EnsureCreated();
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

app.Run();
