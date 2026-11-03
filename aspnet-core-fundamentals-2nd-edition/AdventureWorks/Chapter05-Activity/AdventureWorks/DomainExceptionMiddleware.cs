using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks;

public sealed class DomainExceptionMiddleware(
    IProblemDetailsService problems) : IMiddleware
{
    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (DomainValidationException ex)
            when (!context.Response.HasStarted)
        {
            await WriteAsync(
                context, 400, "Invalid request", ex.Message);
        }
        catch (ResourceNotFoundException ex)
            when (!context.Response.HasStarted)
        {
            await WriteAsync(
                context, 404, "Resource not found", ex.Message);
        }
    }

    private async Task WriteAsync(HttpContext context,
        int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status, Title = title, Detail = detail
        };
        var written = await problems.TryWriteAsync(new()
        {
            HttpContext = context, ProblemDetails = problem
        });
        if (!written) await context.Response.WriteAsync(title);
    }
}
