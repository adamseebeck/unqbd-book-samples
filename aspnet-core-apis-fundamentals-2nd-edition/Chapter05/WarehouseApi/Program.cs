using WarehouseApi.Endpoints;
using WarehouseApi.RoutingConstraints;
using WarehouseApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IWarehouseService, WarehouseService>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IHealthService, HealthService>();

builder.Services.AddRouting(options =>
{
    options.ConstraintMap.Add("sku", typeof(SkuRouteConstraint));
});

var app = builder.Build();

app.MapProductEndpoints();
app.MapWarehouseEndpoints();
app.MapInventoryEndpoints();
app.MapAdjustmentEndpoints();
app.MapHealthEndpoints();

app.Run();
