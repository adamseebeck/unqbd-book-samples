namespace AdventureWorks;

// Local exercise only: these are not payment authentication,
// webhook signature checks or durable idempotency.
public sealed class SecurityHeadersMiddleware : IMiddleware
{
    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        await next(context);
    }
}

public sealed class IdempotencyKeyMiddleware : IMiddleware
{
    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            var key = context.Request.Headers["Idempotency-Key"]
                .ToString();
            if (!IsValid(key))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(
                    "A bounded exercise key is required.");
                return;
            }
            context.Items["IdempotencyKey"] = key;
            context.Response.Headers["Idempotency-Key"] = key;
        }
        await next(context);
    }

    private static bool IsValid(string key) =>
        key.Length is > 0 and <= 64 &&
        key.All(c =>
            char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
