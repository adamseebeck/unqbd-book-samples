namespace AdventureWorks;

public sealed class EndpointLoggingMiddleware(
    RequestDelegate next,
    ILogger<EndpointLoggingMiddleware> logger)
{
    public Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Selected endpoint {Endpoint}",
            context.GetEndpoint()?.DisplayName);
        return next(context);
    }
}
