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
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseMiddleware<RequestTimingMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
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
    var order = orders.GetById(id);
    return order is null ? Results.NotFound() : Results.Ok(order);
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
