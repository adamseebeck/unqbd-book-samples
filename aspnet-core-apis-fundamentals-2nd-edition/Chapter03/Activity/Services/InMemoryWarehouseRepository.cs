using WarehouseApi.Models;

namespace WarehouseApi.Services;

public class InMemoryWarehouseRepository : IWarehouseRepository
{
    // Keys are stored in uppercase, so MAIN and main find the same record, as in Chapter 1
    private readonly Dictionary<(string Sku, string WarehouseCode), int> _stock = new()
    {
        [("SKU-001", "MAIN")] = 150,
        [("SKU-002", "MAIN")] = 8,
        [("SKU-001", "WEST")] = 42
    };
    private readonly Lock _lock = new();

    public Task<int?> GetOnHandQuantityAsync(string sku, string warehouseCode)
    {
        lock (_lock)
        {
            // null: the warehouse holds no stock record for this SKU
            int? quantity = _stock.TryGetValue(Key(sku, warehouseCode), out var onHand) ? onHand : null;
            return Task.FromResult(quantity);
        }
    }

    public Task<IReadOnlyList<StockLevel>> GetAllStockAsync()
    {
        lock (_lock)
        {
            // Copy while holding the lock so callers never enumerate a dictionary that another request is changing
            IReadOnlyList<StockLevel> snapshot = _stock
                .Select(entry => new StockLevel(entry.Key.Sku, entry.Key.WarehouseCode, entry.Value))
                .ToList();
            return Task.FromResult(snapshot);
        }
    }

    private static (string Sku, string WarehouseCode) Key(string sku, string warehouseCode) =>
        (sku.ToUpperInvariant(), warehouseCode.ToUpperInvariant());
}
