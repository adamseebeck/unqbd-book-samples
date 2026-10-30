using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public class InventoryService : IInventoryService
{
    // One Singleton instance serves every request, so one lock guards both stores, as in Chapter 3.
    // SKUs and warehouse codes compare without regard to case, as codes did in Chapters 1 to 3
    private readonly Dictionary<string, ProductSummary> _products = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SKU-001"] = new ProductSummary("SKU-001", "Industrial Shelf Bracket", 12.99m, true),
        ["SKU-002"] = new ProductSummary("SKU-002", "Heavy Duty Casters (Set of 4)", 45.50m, true)
    };

    // The keys hold both parts in uppercase (see Key), so wh-001 and sku-001 find this stock
    private readonly Dictionary<(string WarehouseCode, string Sku), StockLevelResponse> _stock = new()
    {
        [("WH-001", "SKU-001")] = new StockLevelResponse("SKU-001", "WH-001", 150, 20, 130),
        [("WH-001", "SKU-002")] = new StockLevelResponse("SKU-002", "WH-001", 75, 10, 65)
    };

    private readonly Lock _lock = new();

    public Task<ProductSummary?> GetProductAsync(string sku, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _products.TryGetValue(sku, out var product);
            return Task.FromResult(product);
        }
    }

    public Task<StockLevelResponse?> GetStockLevelAsync(string warehouseCode, string sku, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _stock.TryGetValue(Key(warehouseCode, sku), out var stock);
            return Task.FromResult(stock);
        }
    }

    public Task<IReadOnlyList<ProductSummary>> SearchProductsAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
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

            // ToList copies the matches before the lock is released
            return Task.FromResult<IReadOnlyList<ProductSummary>>(query.OrderBy(p => p.Sku).ToList());
        }
    }

    public Task<ProductSummary?> CreateProductAsync(ProductSummary product, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // TryAdd refuses an existing SKU, so a POST never overwrites a product
            var added = _products.TryAdd(product.Sku, product);
            return Task.FromResult<ProductSummary?>(added ? product : null);
        }
    }

    public Task<ProductSummary?> UpdateProductAsync(string sku, ProductSummary product, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // The read and the write happen under one lock, so a PUT that races a DELETE cannot bring the product back
            if (!_products.TryGetValue(sku, out var current))
            {
                return Task.FromResult<ProductSummary?>(null);
            }

            // The stored SKU keeps the form it was created with, whatever case the request used
            var replacement = product with { Sku = current.Sku };
            _products[sku] = replacement;
            return Task.FromResult<ProductSummary?>(replacement);
        }
    }

    public Task<bool> DeleteProductAsync(string sku, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_products.Remove(sku));
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

    private static (string WarehouseCode, string Sku) Key(string warehouseCode, string sku) =>
        (warehouseCode.ToUpperInvariant(), sku.ToUpperInvariant());
}
