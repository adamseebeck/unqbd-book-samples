// LOCAL EXERCISE ONLY: this is not payment authentication, webhook validation or durable idempotency.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddTransient<SecurityHeadersMiddleware>();
builder.Services.AddTransient<IdempotencyKeyMiddleware>();
var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.MapWhen(context => context.Request.Path.StartsWithSegments("/webhooks"), branch =>
{
    branch.UseMiddleware<IdempotencyKeyMiddleware>();
    branch.Run(async context =>
    {
        if (!HttpMethods.IsPost(context.Request.Method) || context.Request.Path != "/webhooks/demo")
        { context.Response.StatusCode = 404; return; }
        await context.Response.WriteAsJsonAsync(new { Key = context.Items["IdempotencyKey"] });
    });
});
app.UseMiddleware<IdempotencyKeyMiddleware>();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/payments") && context.Request.Headers["X-Exercise-Identity"] != "developer")
    { context.Response.StatusCode = 401; return; }
    await next();
});
app.MapPost("/api/payments", (HttpContext context) => Results.Ok(new { Key = context.Items["IdempotencyKey"] }));
app.Run();
