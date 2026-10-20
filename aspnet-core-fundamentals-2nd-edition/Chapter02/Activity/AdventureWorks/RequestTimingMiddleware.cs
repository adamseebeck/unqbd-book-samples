using System.Diagnostics;

public sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            logger.LogInformation("{Method} {Path} elapsed {ElapsedMs} ms",
                context.Request.Method, context.Request.Path,
                timer.Elapsed.TotalMilliseconds);
        }
    }
}
