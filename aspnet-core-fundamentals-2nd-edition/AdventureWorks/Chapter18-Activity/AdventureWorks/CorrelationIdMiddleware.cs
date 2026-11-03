using System.Diagnostics;

namespace AdventureWorks;

public sealed class CorrelationIdMiddleware(
    ILogger<CorrelationIdMiddleware> logger) : IMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        var supplied =
            context.Request.Headers[HeaderName].ToString();
        // Without a usable header, reuse the request's trace ID,
        // so logs, Problem Details and traces share one ID.
        var id = IsValid(supplied) ? supplied
            : Activity.Current?.TraceId.ToHexString()
                ?? Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        });
        var scope = new Dictionary<string, object>
        {
            ["CorrelationId"] = id
        };
        using (logger.BeginScope(scope))
        {
            await next(context);
        }
    }

    private static bool IsValid(string value) =>
        value.Length is > 0 and <= 64 &&
        value.All(c =>
            char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
