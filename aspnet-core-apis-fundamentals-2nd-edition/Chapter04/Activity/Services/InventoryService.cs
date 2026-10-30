using System.Collections.Concurrent;
using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public class InventoryService : IInventoryService
{
    // One Singleton instance serves every request, so both stores must be safe for concurrent access.
    // SKUs and warehouse codes compare without regard to case, as codes did in Chapters 1 to 3
    private readonly ConcurrentDictionary<string, ProductSummary> _products = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SKU-001"] = new ProductSummary("SKU-001", "Industrial Shelf Bracket", 12.99m, true),
        ["SKU-002"] = new ProductSummary("SKU-002", "Heavy Duty Casters (Set of 4)", 45.50m, true)
    };

    // The keys hold both parts in uppercase (see Key), so wh-001 and sku-001 find this stock
    private readonly ConcurrentDictionary<(string WarehouseCode, string Sku), StockLevelResponse> _stock = new()
    {
        [("WH-001", "SKU-001")] = new StockLevelResponse("SKU-001", "WH-001", 150, 20, 130),
        [("WH-001", "SKU-002")] = new StockLevelResponse("SKU-002", "WH-001", 75, 10, 65)
    };

    // Activity: reorder thresholds live in a separate lookup so the stock contract stays unchanged
    private readonly ConcurrentDictionary<(string WarehouseCode, string Sku), int> _reorderThresholds = new()
    {
        [("WH-001", "SKU-001")] = 100,
        [("WH-001", "SKU-002")] = 100
    };

    public Task<ProductSummary?> GetProductAsync(string sku, CancellationToken cancellationToken = default)
    {
        _products.TryGetValue(sku, out var product);
        return Task.FromResult(product);
    }

    public Task<StockLevelResponse?> GetStockLevelAsync(string warehouseCode, string sku, CancellationToken cancellationToken = default)
    {
        _stock.TryGetValue(Key(warehouseCode, sku), out var stock);
        return Task.FromResult(stock);
    }

    public Task<IReadOnlyList<ProductSummary>> SearchProductsAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _products.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        return Task.FromResult<IReadOnlyList<ProductSummary>>(query.OrderBy(p => p.Sku).ToList());
    }

    public Task<ProductSummary?> CreateProductAsync(ProductSummary product, CancellationToken cancellationToken = default)
    {
        // TryAdd refuses an existing SKU, so a POST never overwrites a product
        var added = _products.TryAdd(product.Sku, product);
        return Task.FromResult<ProductSummary?>(added ? product : null);
    }

    public Task<ProductSummary?> UpdateProductAsync(string sku, ProductSummary product, CancellationToken cancellationToken = default)
    {
        while (_products.TryGetValue(sku, out var current))
        {
            // The stored SKU keeps the form it was created with, whatever case the request used
            var replacement = product with { Sku = current.Sku };

            // TryUpdate succeeds only if the entry still equals the value that was read,
            // so a PUT that races a DELETE cannot bring the product back
            if (_products.TryUpdate(sku, replacement, current))
                return Task.FromResult<ProductSummary?>(replacement);
        }

        return Task.FromResult<ProductSummary?>(null);
    }

    public Task<bool> DeleteProductAsync(string sku, CancellationToken cancellationToken = default)
    {
        // TryRemove is atomic: when two requests delete the same SKU, exactly one succeeds
        return Task.FromResult(_products.TryRemove(sku, out _));
    }

    public Task<StockAdjustmentResult> AdjustStockAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var key = Key(request.WarehouseCode, request.Sku);

        while (_stock.TryGetValue(key, out var current))
        {
            var newOnHand = current.OnHand + request.Quantity;
            if (newOnHand < current.Reserved)
                return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.InsufficientStock, current));

            var updated = current with { OnHand = newOnHand, Available = newOnHand - current.Reserved };
            if (_stock.TryUpdate(key, updated, current))
                return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.Applied, updated));
        }

        return Task.FromResult(new StockAdjustmentResult(StockAdjustmentStatus.StockItemNotFound));
    }

    private static (string WarehouseCode, string Sku) Key(string warehouseCode, string sku) =>
        (warehouseCode.ToUpperInvariant(), sku.ToUpperInvariant());

    public Task<IReadOnlyList<LowStockAlert>> GetLowStockItemsAsync(string warehouseCode, CancellationToken cancellationToken = default)
    {
        var alerts = new List<LowStockAlert>();

        var code = warehouseCode.ToUpperInvariant();

        foreach (var stock in _stock.Values.Where(s => s.WarehouseCode == code).OrderBy(s => s.Sku))
        {
            if (_reorderThresholds.TryGetValue(Key(stock.WarehouseCode, stock.Sku), out var threshold)
                && stock.OnHand < threshold)
            {
                alerts.Add(new LowStockAlert(stock.Sku, stock.WarehouseCode, stock.OnHand, threshold));
            }
        }

        return Task.FromResult<IReadOnlyList<LowStockAlert>>(alerts);
    }
}
