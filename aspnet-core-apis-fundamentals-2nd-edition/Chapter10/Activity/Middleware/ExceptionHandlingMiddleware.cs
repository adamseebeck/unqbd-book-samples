using Microsoft.AspNetCore.WebUtilities;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationContext correlationContext)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex) when (!context.Response.HasStarted)
        {
            // In Development, Minimal APIs throw binding failures; they are client errors, not server errors
            _logger.LogWarning(
                "Bad request {CorrelationId}: {Message}",
                correlationContext.CorrelationId,
                ex.Message);

            await WriteProblemAsync(context, new ErrorResponse(
                Type: "about:blank",
                Title: ReasonPhrases.GetReasonPhrase(ex.StatusCode),
                Status: ex.StatusCode,
                Detail: ex.Message,
                Instance: context.Request.Path,
                CorrelationId: correlationContext.CorrelationId));
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected: nothing failed on the server, and nobody is left to read a response
            _logger.LogDebug("Request {CorrelationId} was canceled by the client", correlationContext.CorrelationId);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            _logger.LogError(ex,
                "Unhandled exception occurred processing request {CorrelationId}",
                correlationContext.CorrelationId);

            await WriteProblemAsync(context, new ErrorResponse(
                Type: "https://api.example.com/errors/internal-server-error",
                Title: "Internal Server Error",
                Status: StatusCodes.Status500InternalServerError,
                Detail: "An unexpected error occurred processing your request.",
                Instance: context.Request.Path,
                CorrelationId: correlationContext.CorrelationId));
        }
    }

    private static Task WriteProblemAsync(HttpContext context, ErrorResponse response)
    {
        // Discard any status code and headers the failed endpoint had already set
        context.Response.Clear();
        context.Response.StatusCode = response.Status;

        // Without the contentType argument, WriteAsJsonAsync would send application/json
        return context.Response.WriteAsJsonAsync(response, options: null, contentType: "application/problem+json");
    }
}
