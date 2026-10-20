using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);
builder.Services.AddSingleton<OrderMetrics>();
builder.Services.AddHttpClient("catalog", client =>
    client.BaseAddress = new Uri(builder.Configuration["CatalogUrl"] ?? "http://127.0.0.1:42148/catalog"));
builder.Services.AddHealthChecks().AddCheck<CatalogHealthCheck>("catalog", tags: ["ready"]);
string? otlp = builder.Configuration["Telemetry:OtlpEndpoint"];
builder.Services.AddOpenTelemetry().ConfigureResource(r => r.AddService("AdventureWorks.Orders"))
    .WithTracing(t =>
    {
        t.AddSource("Microsoft.AspNetCore").AddHttpClientInstrumentation()
            .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(
                builder.Configuration.GetValue("Telemetry:SampleRatio", 1.0))));
        if (otlp is null) t.AddConsoleExporter();
        else t.AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(new Uri(otlp.TrimEnd('/') + "/"), "v1/traces");
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
        });
    })
    .WithMetrics(m =>
    {
        m.AddMeter("AdventureWorks.Orders", "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
        if (otlp is null) m.AddConsoleExporter((_, reader) =>
            reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 500);
        else m.AddOtlpExporter((o, reader) =>
        {
            o.Endpoint = new Uri(new Uri(otlp.TrimEnd('/') + "/"), "v1/metrics");
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
            reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 500;
        });
    });
var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.MapGet("/catalog", async (int? delayMs, int? status, CancellationToken ct) =>
{
    await Task.Delay(Math.Clamp(delayMs ?? 0, 0, 2000), ct);
    return Results.StatusCode(status is >= 100 and <= 599 ? status.Value : 200);
});
app.MapPost("/api/orders", async (CreateOrderRequest request, IHttpClientFactory clients,
    OrderMetrics metrics, ILoggerFactory loggers, CancellationToken ct) =>
{
    var region = request.Region is "east" or "west" ? request.Region : "invalid";
    var clock = Stopwatch.StartNew();
    bool success = false;
    try
    {
        if (region == "invalid") return Results.BadRequest();
        using var response = await clients.CreateClient("catalog").GetAsync("", ct);
        success = response.IsSuccessStatusCode;
        return Results.StatusCode(success ? 201 : 503);
    }
    catch (HttpRequestException) { return Results.StatusCode(503); }
    finally
    {
        metrics.Record(region, clock.Elapsed.TotalSeconds, success);
        loggers.CreateLogger("Orders").LogInformation("Order processing completed for {Region}: {Success}",
            region, success);
    }
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResultStatusCodes = { [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable }
});
app.Run();
