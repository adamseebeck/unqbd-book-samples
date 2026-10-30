using Microsoft.AspNetCore.Diagnostics;
using WarehouseApi.Services;

namespace WarehouseApi.ErrorHandling;

// AddExceptionHandler registers this as a Singleton, so per-request services come from HttpContext
public class WarehouseExceptionHandler : IExceptionHandler
{
    private readonly IExceptionToProblemDetailsMapper _mapper;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<WarehouseExceptionHandler> _logger;

    public WarehouseExceptionHandler(
        IExceptionToProblemDetailsMapper mapper,
        IProblemDetailsService problemDetailsService,
        ILogger<WarehouseExceptionHandler> logger)
    {
        _mapper = mapper;
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.RequestServices.GetRequiredService<ICorrelationContext>().CorrelationId;
        var problem = _mapper.Map(exception, httpContext);
        var statusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // The middleware no longer logs an exception that a handler reports as handled, so log it here
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception,
                "Unhandled exception for {Method} {Path}, correlation ID {CorrelationId}",
                httpContext.Request.Method, httpContext.Request.Path, correlationId);
        }
        else
        {
            _logger.LogWarning(
                "{ExceptionType} for {Method} {Path}, correlation ID {CorrelationId}: {Message}",
                exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, correlationId, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        // Writes application/problem+json and runs CustomizeProblemDetails from AddProblemDetails
        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });

        if (!written)
        {
            // The client accepts neither JSON nor Problem Details, so answer with plain text
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync($"{problem.Title}: correlation ID {correlationId}", cancellationToken);
        }

        return true;
    }
}
