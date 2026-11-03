using AdventureWorks.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AdventureWorks;

public static class Observability
{
    // Traces and metrics through OpenTelemetry, and the readiness
    // checks. Telemetry:Exporter picks where the data goes:
    // "console" (this window), "otlp" (Telemetry:OtlpEndpoint, a
    // collector), or nothing, the default. Telemetry:SampleRatio
    // keeps that share of new traces (1.0, all, by default).
    public static IServiceCollection AddStoreObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<OrderTelemetry>();
        var exporter = configuration["Telemetry:Exporter"];
        var endpoint = configuration["Telemetry:OtlpEndpoint"];
        var ratio = configuration.GetValue(
            "Telemetry:SampleRatio", 1.0);
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService("AdventureWorks"))
            .WithTracing(tracing =>
            {
                tracing.SetSampler(new ParentBasedSampler(
                        new TraceIdRatioBasedSampler(ratio)))
                    .AddSource("Microsoft.AspNetCore",
                        OrderTelemetry.Name)
                    .AddHttpClientInstrumentation();
                if (exporter == "console")
                    tracing.AddConsoleExporter();
                else if (exporter == "otlp")
                    tracing.AddOtlpExporter(o =>
                        o.Endpoint = new Uri(endpoint!));
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(OrderTelemetry.Name,
                    "Microsoft.AspNetCore.Hosting",
                    "System.Net.Http");
                // Every 30 s, so a request's spans stay on screen
                // between two dumps of every metric.
                if (exporter == "console")
                    metrics.AddConsoleExporter((_, r) =>
                        r.PeriodicExportingMetricReaderOptions
                            .ExportIntervalMilliseconds = 30_000);
                else if (exporter == "otlp")
                    metrics.AddOtlpExporter(o =>
                        o.Endpoint = new Uri(endpoint!));
            });
        // Ready means the app can serve: its database answers and
        // the warehouse service is reachable within a second.
        services.AddHealthChecks()
            .AddDbContextCheck<AdventureWorksDbContext>(
                "database", tags: ["ready"])
            .AddCheck<InventoryHealthCheck>(
                "inventory", tags: ["ready"]);
        var inventory = new Uri(configuration["Inventory:BaseUrl"]
            ?? "http://127.0.0.1:42132/");
        services.AddHttpClient(InventoryHealthCheck.Client,
            http =>
            {
                http.BaseAddress = inventory;
                http.Timeout = TimeSpan.FromSeconds(1);
            });
        return services;
    }
}
