using WarehouseApi.Models;

namespace WarehouseApi.Services;

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
