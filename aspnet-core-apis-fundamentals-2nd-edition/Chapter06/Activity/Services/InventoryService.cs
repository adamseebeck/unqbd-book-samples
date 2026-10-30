using System.Collections.Concurrent;
using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public class InventoryService : IInventoryService
{
    private readonly IdempotencyContext _idempotencyContext;
    private readonly ConcurrentDictionary<(string WarehouseCode, string ProductSku), StockItem> _stockItems = new();

    // Every write takes this lock, so a check and the change it guards run as one step
    private readonly Lock _writeLock = new();

    public InventoryService(IdempotencyContext idempotencyContext)
    {
        _idempotencyContext = idempotencyContext;

        // Seed sample data: the Chapter 5 products in the Chapter 5 warehouses
        _stockItems[("WH-001", "SKU-001")] = new StockItem("SKU-001", "WH-001", 150, 20, 40);
        _stockItems[("WH-001", "SKU-002")] = new StockItem("SKU-002", "WH-001", 75, 10, 100);
        _stockItems[("WH-002", "SKU-001")] = new StockItem("SKU-001", "WH-002", 60, 0, 25);
    }

    public Task<PagedResult<StockItemResponse>> GetStockAsync(PaginationParameters pagination, StockFilterParameters filters, CancellationToken cancellationToken = default)
    {
        var query = _stockItems.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(filters.WarehouseCode))
            query = query.Where(s => string.Equals(s.WarehouseCode, filters.WarehouseCode, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(filters.ProductSku))
            query = query.Where(s => s.ProductSku == filters.ProductSku);

        if (filters.MinQuantity.HasValue)
            query = query.Where(s => s.OnHand >= filters.MinQuantity.Value);

        if (filters.MaxQuantity.HasValue)
            query = query.Where(s => s.OnHand <= filters.MaxQuantity.Value);

        var sorted = filters.SortBy?.ToLowerInvariant() switch
        {
            "onhand" => filters.SortDescending
                ? query.OrderByDescending(s => s.OnHand)
                : query.OrderBy(s => s.OnHand),
            "available" => filters.SortDescending
                ? query.OrderByDescending(s => s.OnHand - s.Reserved)
                : query.OrderBy(s => s.OnHand - s.Reserved),
            _ => filters.SortDescending
                ? query.OrderByDescending(s => s.ProductSku)
                : query.OrderBy(s => s.ProductSku)
        };

        // The SKU breaks ties, so items with equal values keep their order from page to page;
        // materializing once makes the count and the page come from the same snapshot
        var matches = sorted.ThenBy(s => s.ProductSku).ToList();
        var items = matches
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Select(ToResponse)
            .ToList();

        return Task.FromResult(new PagedResult<StockItemResponse>(
            items, matches.Count, pagination.PageNumber, pagination.PageSize));
    }

    public Task<StockItemResponse?> GetStockItemAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<StockItemResponse?>(
            _stockItems.TryGetValue(Key(warehouseCode, productSku), out var item) ? ToResponse(item) : null);
    }

    public Task<StockAdjustmentResult> AdjustStockAsync(string warehouseCode, string productSku, int quantityChange, string reasonCode, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (quantityChange == 0)
        {
            return Task.FromResult(new StockAdjustmentResult(false, null, "An adjustment must change the quantity", 400));
        }

        var key = Key(warehouseCode, productSku);
        var requestHash = _idempotencyContext.CreateRequestHash(
            $"adjust|{key.WarehouseCode}|{key.ProductSku}|{quantityChange}|{reasonCode}");

        lock (_writeLock)
        {
            if (_idempotencyContext.TryGetEntry(idempotencyKey, out var existingEntry))
            {
                // A retry gets the stored result; the same key with another payload is a client error
                return Task.FromResult(existingEntry.RequestHash == requestHash
                    ? new StockAdjustmentResult(true, (StockItemResponse)existingEntry.Response, null, existingEntry.StatusCode)
                    : new StockAdjustmentResult(false, null, "Idempotency key reused with a different request payload", 422));
            }

            if (!_stockItems.TryGetValue(key, out var item))
            {
                return Task.FromResult(new StockAdjustmentResult(false, null, "Stock item not found", 404));
            }

            var newOnHand = item.OnHand + quantityChange;
            if (newOnHand < item.Reserved)
            {
                return Task.FromResult(new StockAdjustmentResult(
                    false, null, "Adjustment would leave fewer units on hand than are reserved", 409));
            }

            var updated = item with { OnHand = newOnHand };
            _stockItems[key] = updated;

            // The adjustment changed a stock item that already existed: 200 with its new state
            var response = ToResponse(updated);
            _idempotencyContext.Store(idempotencyKey, response, 200, requestHash);

            return Task.FromResult(new StockAdjustmentResult(true, response, null, 200));
        }
    }

    public Task<StockAdjustmentResult> CreateStockAsync(string warehouseCode, CreateStockItemRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var key = Key(warehouseCode, request.ProductSku);
        var requestHash = _idempotencyContext.CreateRequestHash(
            $"create|{key.WarehouseCode}|{key.ProductSku}|{request.OnHand}|{request.Reserved}|{request.ReorderThreshold}");

        lock (_writeLock)
        {
            if (_idempotencyContext.TryGetEntry(idempotencyKey, out var existingEntry))
            {
                // A retry gets the stored 201 response; the same key with another payload is a client error
                return Task.FromResult(existingEntry.RequestHash == requestHash
                    ? new StockAdjustmentResult(true, (StockItemResponse)existingEntry.Response, null, existingEntry.StatusCode)
                    : new StockAdjustmentResult(false, null, "Idempotency key reused with a different request payload", 422));
            }

            var item = new StockItem(key.ProductSku, key.WarehouseCode, request.OnHand, request.Reserved, request.ReorderThreshold);
            if (!_stockItems.TryAdd(key, item))
            {
                return Task.FromResult(new StockAdjustmentResult(false, null, "The warehouse already holds stock of this SKU", 409));
            }

            var response = ToResponse(item);
            _idempotencyContext.Store(idempotencyKey, response, 201, requestHash);

            return Task.FromResult(new StockAdjustmentResult(true, response, null, 201));
        }
    }

    public Task<bool> UpdateStockAsync(string warehouseCode, string productSku, int onHand, int reserved, int reorderThreshold, CancellationToken cancellationToken = default)
    {
        var key = Key(warehouseCode, productSku);

        lock (_writeLock)
        {
            if (!_stockItems.TryGetValue(key, out var item))
            {
                return Task.FromResult(false);
            }

            _stockItems[key] = item with { OnHand = onHand, Reserved = reserved, ReorderThreshold = reorderThreshold };
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteStockAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default)
    {
        lock (_writeLock)
        {
            return Task.FromResult(_stockItems.TryRemove(Key(warehouseCode, productSku), out _));
        }
    }

    // Warehouse codes compare without regard to case (Chapter 5), so the keys hold them in uppercase
    private static (string WarehouseCode, string ProductSku) Key(string warehouseCode, string productSku) =>
        (warehouseCode.ToUpperInvariant(), productSku);

    private static StockItemResponse ToResponse(StockItem item) => new(
        item.ProductSku, item.WarehouseCode, item.OnHand, item.Reserved, item.OnHand - item.Reserved, item.ReorderThreshold);

    private record StockItem(string ProductSku, string WarehouseCode, int OnHand, int Reserved, int ReorderThreshold);
}
