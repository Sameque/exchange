using OrderGenerator.API.Middleware;
using OrderGenerator.API.Observability;
using OrderGenerator.Application.UseCases;
using OrderGenerator.Infrastructure.Configuration;
using OrderGenerator.Infrastructure.Exchange;
using OrderGenerator.Infrastructure.Extensions;
using OrderGenerator.Infrastructure.Persistence;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ExchangeSettings>(
    builder.Configuration.GetSection(ExchangeSettings.SectionName));

builder.Services.AddInfrastructure(builder.Configuration);

builder.AddOpenTelemetry();

builder.Services.AddCors(options =>
{
   options.AddPolicy("AllowAngularDev",
       policy =>
       {
           policy.WithOrigins("http://localhost:4200")
                 .AllowAnyHeader()
                 .AllowAnyMethod();
       });
});

builder.Services.AddScoped<PlaceOrderUseCase>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "OrderGenerator API",
        Version = "v1",
        Description = "REST API for stock order registration via FIX 4.4 protocol."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var settings = new SessionSettings("initiator.cfg");
var application = new FixApplication();
var storeFactory = new FileStoreFactory(settings);
var logFactory = new FileLogFactory(settings);

var initiator = new SocketInitiator(
    application,
    storeFactory,
    settings,
    logFactory);

initiator.Start();

builder.Services.AddSingleton((a) =>
{
    return application;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderGenerator API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowAngularDev");
app.MapControllers();
app.MapPrometheusScrapingEndpoint();

app.Run();
