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

    private static (string Sku, string WarehouseCode) Key(string sku, string warehouseCode) =>
        (sku.ToUpperInvariant(), warehouseCode.ToUpperInvariant());
}
