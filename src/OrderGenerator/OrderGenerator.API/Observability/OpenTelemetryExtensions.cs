using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OrderGenerator.API.Observability;

/// <summary>
/// Wires OpenTelemetry traces and metrics for the OrderGenerator service.
/// Traces are exported via OTLP gRPC to the collector. The collector fans
/// them out to Tempo. Metrics use a Prometheus pull endpoint on /metrics
/// scraped by Prometheus directly (no collector hop).
/// </summary>
internal static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        var otelEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";
        var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "ordergenerator.api";

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(serviceName: serviceName))
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(otelEndpoint);
                    opts.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        return builder;
    }
}
