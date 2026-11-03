using AdventureWorks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseMiddleware<RequestTimingMiddleware>();
if (app.Environment.IsDevelopment())
    app.UseMiddleware<EnvironmentLoggingMiddleware>();
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
app.MapGet("/api/config", () => Results.Ok(new
{
    Environment = app.Environment.EnvironmentName,
    Urls = app.Urls
}));
app.Run();
