using WarehouseApi.Endpoints;
using WarehouseApi.Middleware;
using WarehouseApi.RoutingConstraints;
using WarehouseApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Built-in Minimal API validation; AddProblemDetails gives its 400 responses a Problem Details body
builder.Services.AddValidation();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

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

builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IWarehouseService, WarehouseService>();
builder.Services.AddSingleton<IHealthService, HealthService>();
builder.Services.AddSingleton<IdempotencyContext>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

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

app.MapControllers();
app.MapProductEndpoints();
app.MapWarehouseEndpoints();
app.MapStockEndpoints();
app.MapHealthEndpoints();

app.Run();
