using WarehouseApi.Endpoints;
using WarehouseApi.RoutingConstraints;
using WarehouseApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IWarehouseService, WarehouseService>();
builder.Services.AddSingleton<IHealthService, HealthService>();

// Both hold state that every request must share
builder.Services.AddSingleton<IdempotencyContext>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();

builder.Services.AddRouting(options =>
{
    options.ConstraintMap.Add("sku", typeof(SkuRouteConstraint));
});

var app = builder.Build();

app.MapProductEndpoints();
app.MapWarehouseEndpoints();
app.MapStockEndpoints();
app.MapHealthEndpoints();

app.Run();
