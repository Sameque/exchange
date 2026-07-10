using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Shared;
using OrderGenerator.API.Fix;
using OrderGenerator.API.Middleware;
using OrderGenerator.Application.UseCases;
using OrderGenerator.Domain.Interfaces;
using OrderGenerator.Infrastructure.Exchange;
using OrderGenerator.Infrastructure.Extensions;
using OrderGenerator.Infrastructure.Persistence;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, migrationsAssembly: typeof(Program).Assembly.GetName().Name);

builder.AddSharedOpenTelemetry();

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

builder.Services.AddSingleton<FixApplication>();
builder.Services.AddSingleton<IFixApplication>(sp => sp.GetRequiredService<FixApplication>());
builder.Services.AddHostedService<FixHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
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
