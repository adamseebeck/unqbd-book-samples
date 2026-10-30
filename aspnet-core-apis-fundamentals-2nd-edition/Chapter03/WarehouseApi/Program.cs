using Microsoft.Extensions.Options;
using WarehouseApi.Configuration;
using WarehouseApi.Services;

// Application configuration
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IWarehouseService, WarehouseService>();

// Options bound to configuration sections and validated while the host starts
builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection("Database"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<WarehouseFeatureOptions>()
    .Bind(builder.Configuration.GetSection("WarehouseFeatures"))
    .ValidateDataAnnotations()
    .Validate(features => features.DefaultPageSize <= features.MaxPageSize,
        "WarehouseFeatures:DefaultPageSize must not be greater than MaxPageSize.")
    .ValidateOnStart();

// Singleton: shared state across all requests
builder.Services.AddSingleton<IWarehouseRepository, InMemoryWarehouseRepository>();

// Scoped: per-request business logic and the request log it writes to
builder.Services.AddScoped<InventoryRequestLog>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

// CORRECT: Singleton depending on Singleton
builder.Services.AddSingleton<FixedWarehouseService>();

// Factory registration for complex initialization
// The container tracks the returned instance for disposal
builder.Services.AddSingleton<IStockAlertService>(serviceProvider =>
{
    // A client that lives as long as the application must still pick up DNS changes
    var handler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) };
    var client = new HttpClient(handler) { BaseAddress = new Uri("https://alerts.example.com") };
    return new StockAlertService(client);
});

// ANTI-PATTERN: Singleton depending on Scoped
// Uncomment this line to watch scope validation stop the application at startup
// builder.Services.AddSingleton<CaptiveDependencyDemoService>();

// Scope validation for Development (WebApplication enables both checks there by default)
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

var app = builder.Build();

// Behind a reverse proxy, UseForwardedHeaders() restores the client's scheme and IP address only
// after ForwardedHeadersOptions names the headers (XForwardedFor, XForwardedProto) and the proxy's
// address; by default it processes no headers. Chapter 16 configures it.

// API endpoints
app.MapGet("/api/warehouses/{code}", async (string code, IWarehouseService service) =>
{
    var summary = await service.GetSummaryAsync(code);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});

// Diagnostic endpoint: what the configuration resolved to in this environment
app.MapGet("/api/health/config", (
    IOptions<DatabaseOptions> dbOptions,
    IOptions<WarehouseFeatureOptions> featureOptions) =>
{
    var db = dbOptions.Value;
    var features = featureOptions.Value;

    return Results.Ok(new
    {
        DatabaseConfigured = !string.IsNullOrEmpty(db.ConnectionString),
        CommandTimeout = db.CommandTimeoutSeconds,
        RealTimeInventoryEnabled = features.EnableRealTimeInventory,
        DefaultPageSize = features.DefaultPageSize,
        AllowedOriginCount = features.AllowedOrigins.Length
    });
});

// Minimal API endpoint using the scoped service
app.MapGet("/api/inventory/{sku}", async (
    string sku,
    string warehouseCode,
    IInventoryService inventoryService) =>
{
    var quantity = await inventoryService.GetStockQuantityAsync(sku, warehouseCode);
    return quantity is null
        ? Results.NotFound()
        : Results.Ok(new { Sku = sku, WarehouseCode = warehouseCode, Quantity = quantity });
});

app.Run();
