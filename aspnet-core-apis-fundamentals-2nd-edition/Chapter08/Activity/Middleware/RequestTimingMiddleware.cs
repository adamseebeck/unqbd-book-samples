using System.Diagnostics;
using WarehouseApi.Services;

namespace WarehouseApi.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(
        RequestDelegate next,
        ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationContext correlationContext)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            // Runs for failed requests too, so slow failures show up in the log
            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} completed in {ElapsedMilliseconds} ms (correlation ID {CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                correlationContext.CorrelationId);
        }
    }
}
