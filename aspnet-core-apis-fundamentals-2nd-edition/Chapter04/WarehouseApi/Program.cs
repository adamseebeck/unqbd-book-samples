using WarehouseApi.Endpoints;
using WarehouseApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IHealthService, HealthService>();

var app = builder.Build();

app.MapInventoryEndpoints();
app.MapHealthEndpoints();
app.MapAdjustmentEndpoints();

app.Run();
