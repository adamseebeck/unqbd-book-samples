using Microsoft.Extensions.Options;
using WarehouseApi.Configuration;
using WarehouseApi.Models;
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

// Activity: inventory thresholds; the notification email comes from user secrets in Development
builder.Services.AddOptions<InventoryThresholdOptions>()
    .Bind(builder.Configuration.GetSection("InventoryThresholds"))
    .ValidateDataAnnotations()
    .Validate(thresholds => thresholds.CriticalStockThreshold < thresholds.LowStockThreshold,
        "InventoryThresholds:CriticalStockThreshold must be lower than LowStockThreshold.")
    .ValidateOnStart();

var app = builder.Build();

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
        CommandTimeout = db.CommandTimeoutSeconds,
        RealTimeInventoryEnabled = features.EnableRealTimeInventory,
        DefaultPageSize = features.DefaultPageSize,
        AllowedOriginCount = features.AllowedOrigins.Length
    });
});

// A read-only check, so GET: sku and quantity are not in the route, so they bind from the query string
app.MapGet("/api/inventory/stock-status", (
    string sku,
    int quantity,
    IOptions<InventoryThresholdOptions> thresholdOptions) =>
{
    var thresholds = thresholdOptions.Value;

    var status = quantity <= thresholds.CriticalStockThreshold ? "Critical"
        : quantity <= thresholds.LowStockThreshold ? "Low"
        : "Normal";

    return Results.Ok(new StockStatusResponse(
        sku,
        quantity,
        status,
        status == "Normal" ? null : thresholds.NotificationEmail));
});

app.Run();
