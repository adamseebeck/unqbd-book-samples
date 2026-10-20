using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public sealed class DomainValidationException(string message) : Exception(message);
public sealed class ResourceNotFoundException(string message) : Exception(message);

public sealed class CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var supplied = context.Request.Headers["X-Correlation-ID"].ToString();
        var id = supplied.Length is > 0 and <= 64 &&
            supplied.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? supplied : Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = id;
            return Task.CompletedTask;
        });
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await next(context);
        }
    }
}

public sealed class RequestTimingMiddleware(ILogger<RequestTimingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var started = Stopwatch.GetTimestamp();
        try { await next(context); }
        finally
        {
            logger.LogInformation("{Method} {Path} elapsed {ElapsedMs} ms",
                context.Request.Method, context.Request.Path,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}

public sealed class DomainExceptionMiddleware(IProblemDetailsService problems) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (DomainValidationException ex) when (!context.Response.HasStarted)
        {
            await WriteAsync(context, 400, "Invalid request", ex.Message);
        }
        catch (ResourceNotFoundException ex) when (!context.Response.HasStarted)
        {
            await WriteAsync(context, 404, "Resource not found", ex.Message);
        }
    }

    private async Task WriteAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        var written = await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
        if (!written) await context.Response.WriteAsync(title);
    }
}
