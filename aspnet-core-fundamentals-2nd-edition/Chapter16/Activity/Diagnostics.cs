using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed record CreateOrderRequest(string Region);
public sealed class CorrelationIdMiddleware(RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers["X-Correlation-ID"].ToString();
        var safe = supplied is { Length: > 0 and <= 64 } &&
            supplied.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-');
        var id = safe ? supplied : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = id;
        context.Response.Headers["X-Correlation-ID"] = id;
        using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = id }))
            await next(context);
    }
}
public sealed class OrderMetrics : IDisposable
{
    private readonly Meter _meter = new("AdventureWorks.Orders", "1.0");
    private readonly Counter<long> _created;
    private readonly Counter<long> _attempts;
    private readonly Histogram<double> _duration;
    public OrderMetrics()
    {
        _attempts = _meter.CreateCounter<long>("order.attempts");
        _created = _meter.CreateCounter<long>("orders.created", unit: "{order}");
        _duration = _meter.CreateHistogram<double>("order.processing.duration", unit: "s");
    }
    public void Attempt() => _attempts.Add(1, new KeyValuePair<string, object?>("endpoint", "/api/orders"));
    public void Record(string region, double seconds, bool success)
    {
        if (success) _created.Add(1, new KeyValuePair<string, object?>("region", region));
        _duration.Record(seconds, new KeyValuePair<string, object?>("success", success));
    }
    public void Dispose() => _meter.Dispose();
}
public sealed class CatalogHealthCheck(IHttpClientFactory clients) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(1));
        var clock = Stopwatch.StartNew();
        try
        {
            using var response = await clients.CreateClient("catalog").GetAsync("", timeout.Token);
            if (!response.IsSuccessStatusCode) return HealthCheckResult.Unhealthy("Catalog rejected probe");
            return clock.ElapsedMilliseconds > 500 ? HealthCheckResult.Degraded("Catalog slow") :
                HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("Catalog probe timed out");
        }
        catch (HttpRequestException)
        {
            return HealthCheckResult.Unhealthy("Catalog unreachable");
        }
    }
}
