using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
    {
        // Keep a caller's ID only when it is short and plain; otherwise start a new one.
        var incoming = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        var correlationId = IsUsable(incoming) ? incoming : Guid.NewGuid().ToString();

        // SetCorrelationId is synchronous, so the value it stores is still set when _next runs below.
        correlationContext.SetCorrelationId(correlationId);
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // ASP.NET Core traces the request with an Activity (W3C trace context); the tag ties the trace to this ID.
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    private static bool IsUsable([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 100
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
