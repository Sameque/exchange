using System.Net;
using System.Text.Json;

namespace OrderGenerator.API.Middleware;

/// <summary>
/// Global exception handler that catches unhandled exceptions
/// and returns a consistent ProblemDetails response.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid argument"),
            InvalidOperationException => (HttpStatusCode.UnprocessableEntity, "Business rule violation"),
            OperationCanceledException => (HttpStatusCode.ServiceUnavailable, "Request cancelled"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = ex.Message,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
