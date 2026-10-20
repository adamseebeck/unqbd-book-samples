public sealed class EnvironmentLoggingMiddleware(RequestDelegate next, IHostEnvironment environment,
    ILogger<EnvironmentLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Exercise environment {Environment}", environment.EnvironmentName);
        await next(context);
    }
}
