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

app.Run();
