var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items["CorrelationId"]?.ToString();
    };
});
builder.Services.AddTransient<CorrelationIdMiddleware>();
builder.Services.AddTransient<RequestTimingMiddleware>();
builder.Services.AddTransient<DomainExceptionMiddleware>();
var app = builder.Build();

if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
else app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapWhen(context => context.Request.Path == "/health", health =>
{
    health.Run(context => context.Response.WriteAsync("Healthy"));
});
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseStatusCodePages();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseRouting();
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api =>
{
    api.Use(async (context, next) =>
    {
        context.Response.Headers["X-API-Pipeline"] = "true";
        await next();
    });
});
app.MapGet("/api/products/{id:int}", (int id) =>
{
    if (id <= 0) throw new DomainValidationException("Product ID must be positive.");
    if (id == 500) throw new InvalidOperationException("Simulated internal failure.");
    if (id > 1000) throw new ResourceNotFoundException($"Product {id} was not found.");
    return Results.Ok(new { Id = id, Name = "AdventureWorks Helmet", Price = 29.99m });
});
app.Run();
