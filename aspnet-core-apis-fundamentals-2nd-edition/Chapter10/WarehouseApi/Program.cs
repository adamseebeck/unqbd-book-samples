using WarehouseApi.Configuration;
using WarehouseApi.Endpoints;
using WarehouseApi.Middleware;
using WarehouseApi.Persistence;
using WarehouseApi.RoutingConstraints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Built-in Minimal API validation; AddProblemDetails gives its 400 responses a Problem Details body
builder.Services.AddValidation();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("IntegrationClients", policy =>
    {
        policy.WithOrigins(
                "https://warehouse-portal.example.com",
                "https://erp-integration.example.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Correlation-ID");
    });
});

// Request-scoped helpers, then the DbContext and the EF Core service implementations
builder.Services.AddWarehouseServices();
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddRouting(options =>
{
    options.ConstraintMap.Add("sku", typeof(SkuRouteConstraint));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // A scope of its own, so the seeding DbContext is disposed before the first request
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    await DbInitializer.SeedAsync(context);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

// Route matching now runs inside the middleware registered above
app.UseRouting();

app.UseCors("IntegrationClients");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // Serves the OpenAPI document at /openapi/v1.json
    app.MapOpenApi();
}

app.MapControllers();
app.MapProductEndpoints();
app.MapWarehouseEndpoints();
app.MapStockEndpoints();
app.MapHealthEndpoints();

app.Run();
