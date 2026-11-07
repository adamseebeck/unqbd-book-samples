using AdventureWorks;
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddOptions<FeatureFlags>()
    .Bind(builder.Configuration.GetSection("FeatureFlags"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<ICacheService,
    MemoryCacheService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddTransient<IEmailValidator, EmailValidator>();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items["CorrelationId"]);
builder.Services.AddTransient<CorrelationIdMiddleware>();
builder.Services.AddTransient<DomainExceptionMiddleware>();
builder.Services.AddTransient<SecurityHeadersMiddleware>();
builder.Services.AddTransient<IdempotencyKeyMiddleware>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.MapWhen(context => context.Request.Path == "/ping",
    ping => ping.Run(context =>
        context.Response.WriteAsync("pong")));
app.MapWhen(context =>
    context.Request.Path.StartsWithSegments("/webhooks"),
    webhooks =>
    {
        webhooks.UseMiddleware<IdempotencyKeyMiddleware>();
        webhooks.Run(async context =>
        {
            if (!HttpMethods.IsPost(context.Request.Method) ||
                context.Request.Path != "/webhooks/demo")
            {
                context.Response.StatusCode = 404;
                return;
            }
            await context.Response.WriteAsJsonAsync(
                new { Key = context.Items["IdempotencyKey"] });
        });
    });
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseMiddleware<IdempotencyKeyMiddleware>();
app.Use(async (context, next) =>
{
    // A local exercise guard, not authentication.
    var identity = context.Request.Headers["X-Exercise-Identity"];
    if (context.Request.Path.StartsWithSegments("/api/payments")
        && identity != "developer")
    {
        context.Response.StatusCode = 401;
        return;
    }
    await next();
});
app.UseRouting();
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api"),
    api => api.Use(async (context, next) =>
    {
        context.Response.Headers["X-API-Pipeline"] = "true";
        await next();
    }));
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/api/status"));
app.MapGet("/api/status", () => Results.Ok(new
{
    Status = "Operational",
    Timestamp = DateTime.UtcNow
}));
app.MapGet("/api/orders/{id:int}", (int id,
    IOrderRepository orders, IOptions<FeatureFlags> flags) =>
{
    if (!flags.Value.EnableNewCheckout)
        return Results.StatusCode(
            StatusCodes.Status503ServiceUnavailable);
    if (id <= 0)
        throw new DomainValidationException(
            "Order ID must be positive.");
    return Results.Ok(orders.GetById(id) ??
        throw new ResourceNotFoundException(
            $"Order {id} was not found."));
});
app.MapPost("/api/payments", (HttpContext context) =>
    Results.Ok(new { Key = context.Items["IdempotencyKey"] }));
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/config", (IOptions<FeatureFlags> flags) =>
        Results.Ok(flags.Value));
    app.MapGet("/api/email-check",
        (string address, IEmailValidator validator) => Results.Ok(
            new { IsValid = validator.IsValid(address) }));
}
app.Run();
