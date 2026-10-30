using WarehouseApi.Configuration;
using WarehouseApi.Endpoints;
using WarehouseApi.Middleware;
using WarehouseApi.RoutingConstraints;

var builder = WebApplication.CreateBuilder(args);

// AddControllers registered these; UseAuthentication and UseAuthorization still need them
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
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

// The application services, their lifetimes and the store behind them
builder.Services.AddWarehouseServices();

builder.Services.AddRouting(options =>
{
    options.ConstraintMap.Add("sku", typeof(SkuRouteConstraint));
});

var app = builder.Build();

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

app.MapOrderEndpoints();
app.MapProductEndpoints();
app.MapWarehouseEndpoints();
app.MapStockEndpoints();
app.MapHealthEndpoints();

app.Run();
