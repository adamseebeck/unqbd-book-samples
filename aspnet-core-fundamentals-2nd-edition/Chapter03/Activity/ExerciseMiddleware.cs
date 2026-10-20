public sealed class SecurityHeadersMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        await next(context);
    }
}
public sealed class IdempotencyKeyMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            var key = context.Request.Headers["Idempotency-Key"].ToString();
            if (key.Length is < 1 or > 64 || !key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("A bounded exercise key is required.");
                return;
            }
            context.Items["IdempotencyKey"] = key;
            context.Response.Headers["Idempotency-Key"] = key;
        }
        await next(context);
    }
}
