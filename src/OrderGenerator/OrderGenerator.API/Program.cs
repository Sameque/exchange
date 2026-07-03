using OrderGenerator.API.Middleware;
using OrderGenerator.Application.UseCases;
using OrderGenerator.Infrastructure.Configuration;
using OrderGenerator.Infrastructure.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ExchangeSettings>(
    builder.Configuration.GetSection(ExchangeSettings.SectionName));

builder.Services.AddInfrastructure();

builder.Services.AddScoped<PlaceOrderUseCase>();
builder.Services.AddScoped<GetSymbolsUseCase>();
builder.Services.AddScoped<GetExposureUseCase>();

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

var app = builder.Build();

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

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
