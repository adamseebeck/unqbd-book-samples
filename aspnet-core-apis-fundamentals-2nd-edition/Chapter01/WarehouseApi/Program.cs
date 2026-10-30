// Application configuration
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IWarehouseService, WarehouseService>();

var app = builder.Build();

// API endpoints
app.MapGet("/api/warehouses/{code}", async (string code, IWarehouseService service) =>
{
    var summary = await service.GetSummaryAsync(code);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});

app.Run();

// Domain contract
public record WarehouseSummary(
    string Code,
    string Name,
    string Location,
    bool IsOperational,
    int TotalStockItems
);

// Service abstraction
public interface IWarehouseService
{
    Task<WarehouseSummary?> GetSummaryAsync(string warehouseCode);
}

// Service implementation
public class WarehouseService : IWarehouseService
{
    // Simulated data store, created once and only read afterward
    private static readonly Dictionary<string, WarehouseSummary> Warehouses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["MAIN"] = new WarehouseSummary(
                "MAIN",
                "Main Distribution Center",
                "Chicago, IL",
                true,
                15420),
            ["WEST"] = new WarehouseSummary(
                "WEST",
                "West Coast Facility",
                "Phoenix, AZ",
                true,
                8320)
        };

    public Task<WarehouseSummary?> GetSummaryAsync(string warehouseCode)
    {
        if (string.IsNullOrWhiteSpace(warehouseCode))
        {
            return Task.FromResult<WarehouseSummary?>(null);
        }

        Warehouses.TryGetValue(warehouseCode, out var summary);
        return Task.FromResult(summary);
    }
}
