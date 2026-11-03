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

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
app.MapWhen(context => context.Request.Path == "/ping",
    ping => ping.Run(context =>
        context.Response.WriteAsync("pong")));
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<DomainExceptionMiddleware>();
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
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/config", (IOptions<FeatureFlags> flags) =>
        Results.Ok(flags.Value));
    app.MapGet("/api/email-check",
        (string address, IEmailValidator validator) => Results.Ok(
            new { IsValid = validator.IsValid(address) }));
}
app.Run();
