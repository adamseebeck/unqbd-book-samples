using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public class InventoryService : IInventoryService
{
    // One Singleton instance serves every request, so one lock guards the store, as in Chapter 4
    private readonly Dictionary<(string WarehouseCode, string Sku), StockLevelResponse> _stock = new()
    {
        [("WH-001", "SKU-001")] = new StockLevelResponse("SKU-001", "WH-001", 150, 20, 130),
        [("WH-001", "SKU-002")] = new StockLevelResponse("SKU-002", "WH-001", 75, 10, 65)
    };

    private readonly Lock _lock = new();

    public Task<StockLevelResponse?> GetStockLevelAsync(string warehouseCode, string sku, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _stock.TryGetValue(Key(warehouseCode, sku), out var stock);
            return Task.FromResult(stock);
        }
    }

    public Task<StockAdjustmentResult> AdjustStockAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = Key(request.WarehouseCode, request.Sku);

            if (!_stock.TryGetValue(key, out var current))
            {
                return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.StockItemNotFound));
            }

            var newOnHand = current.OnHand + request.Quantity;
            if (newOnHand < current.Reserved)
            {
                return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.InsufficientStock, current));
            }

            var updated = current with { OnHand = newOnHand, Available = newOnHand - current.Reserved };
            _stock[key] = updated;
            return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.Applied, updated));
        }
    }

    // Warehouse codes still compare without regard to case, so the keys hold them in uppercase;
    // SKUs now have one uppercase format (the sku route constraint), so they are compared exactly
    private static (string WarehouseCode, string Sku) Key(string warehouseCode, string sku) =>
        (warehouseCode.ToUpperInvariant(), sku);
}
