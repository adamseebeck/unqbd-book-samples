using WarehouseApi.Endpoints;
using WarehouseApi.RoutingConstraints;
using WarehouseApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IWarehouseService, WarehouseService>();
builder.Services.AddSingleton<IHealthService, HealthService>();
builder.Services.AddSingleton<IdempotencyContext>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();

// One shared store; a new service instance for each request
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddRouting(options =>
{
    options.ConstraintMap.Add("sku", typeof(SkuRouteConstraint));
});

var app = builder.Build();

app.MapControllers();

// Existing Minimal API endpoints remain operational
app.MapProductEndpoints();
app.MapWarehouseEndpoints();
app.MapStockEndpoints();
app.MapHealthEndpoints();

app.Run();
