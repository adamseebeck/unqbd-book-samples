using Microsoft.AspNetCore.Mvc.Filters;

namespace AdventureWorks;

public sealed class RequestLoggingFilter(
    ILogger<RequestLoggingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        logger.LogInformation("Starting action {Action}",
            context.ActionDescriptor.DisplayName);
        await next();
    }
}

public sealed class RequestLoggingEndpointFilter(
    ILogger<RequestLoggingEndpointFilter> logger)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        logger.LogInformation("Starting endpoint {Endpoint}",
            context.HttpContext.GetEndpoint()?.DisplayName);
        return await next(context);
    }
}
