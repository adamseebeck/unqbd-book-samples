namespace AdventureWorks;

public sealed class EnvironmentLoggingMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    ILogger<EnvironmentLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Environment {Environment}",
            environment.EnvironmentName);
        await next(context);
    }
}
