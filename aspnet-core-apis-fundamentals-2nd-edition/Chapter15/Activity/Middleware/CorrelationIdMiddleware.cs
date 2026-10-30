using WarehouseApi.Services;

namespace WarehouseApi.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const int MaxCorrelationIdLength = 64;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationContext correlationContext)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        correlationContext.SetCorrelationId(correlationId);

        using (_logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                {
                    context.Response.Headers.Append(CorrelationIdHeader, correlationId);
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(
            CorrelationIdHeader,
            out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            var id = headerValue.ToString();

            if (id.Length <= MaxCorrelationIdLength &&
                id.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_'))
            {
                return id;
            }

            // Log the length, never the value: a client could put anything in the header, log lines included
            _logger.LogWarning(
                "Ignored an invalid X-Correlation-ID header of {Length} characters",
                id.Length);
        }

        return Guid.NewGuid().ToString("N");
    }
}
