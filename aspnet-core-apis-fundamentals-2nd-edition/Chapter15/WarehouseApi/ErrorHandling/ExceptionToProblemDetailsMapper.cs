using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using WarehouseApi.Exceptions;

namespace WarehouseApi.ErrorHandling;

public class ExceptionToProblemDetailsMapper : IExceptionToProblemDetailsMapper
{
    private readonly IHostEnvironment _environment;

    public ExceptionToProblemDetailsMapper(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ProblemDetails Map(Exception exception, HttpContext context) => exception switch
    {
        DomainException domainException => MapDomainException(domainException, context),
        BadHttpRequestException badRequest => MapBadRequest(badRequest, context),
        _ => MapUnexpectedException(exception, context)
    };

    private static ProblemDetails MapDomainException(DomainException exception, HttpContext context)
    {
        var problem = new ProblemDetails
        {
            Type = exception.ErrorType,
            Title = exception.Title,
            Status = exception.StatusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        if (exception is ConflictException conflict)
        {
            problem.Extensions["conflictReason"] = conflict.ConflictReason;
        }

        switch (exception)
        {
            case InsufficientStockException stock:
                problem.Extensions["sku"] = stock.Sku;
                problem.Extensions["warehouseCode"] = stock.WarehouseCode;
                problem.Extensions["requestedQuantity"] = stock.RequestedQuantity;
                problem.Extensions["availableQuantity"] = stock.AvailableQuantity;
                break;

            case NotFoundException notFound:
                problem.Extensions["resourceType"] = notFound.ResourceType;
                problem.Extensions["resourceId"] = notFound.ResourceId;
                break;

            case DomainValidationException validation:
                problem.Extensions["errors"] = validation.Errors;
                break;
        }

        return problem;
    }

    // In Development, Minimal APIs throw BadHttpRequestException for binding failures: a client error
    private static ProblemDetails MapBadRequest(BadHttpRequestException exception, HttpContext context) => new()
    {
        Type = "about:blank",
        Title = ReasonPhrases.GetReasonPhrase(exception.StatusCode),
        Status = exception.StatusCode,
        Detail = exception.Message,
        Instance = context.Request.Path
    };

    private ProblemDetails MapUnexpectedException(Exception exception, HttpContext context)
    {
        var isDevelopment = _environment.IsDevelopment();

        var problem = new ProblemDetails
        {
            Type = "https://api.example.com/errors/internal-server-error",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = isDevelopment ? exception.Message : "An unexpected error occurred processing your request.",
            Instance = context.Request.Path
        };

        // Exception details help a developer; outside Development they would help an attacker
        if (isDevelopment)
        {
            problem.Extensions["exceptionType"] = exception.GetType().FullName;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        return problem;
    }
}
