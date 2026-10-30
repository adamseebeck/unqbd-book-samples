using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Models;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class InventoryService : IInventoryService
{
    private readonly InMemoryWarehouseStore _store;
    private readonly IdempotencyContext _idempotencyContext;

    public InventoryService(InMemoryWarehouseStore store, IdempotencyContext idempotencyContext)
    {
        _store = store;
        _idempotencyContext = idempotencyContext;
    }

    public Task<PagedResult<StockItemResponse>> GetStockAsync(PaginationParameters pagination, StockFilterParameters filters, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var query = _store.StockItems.Values.AsEnumerable();

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
                .Select(s => s.ToResponse())
                .ToList();

            return Task.FromResult(new PagedResult<StockItemResponse>(
                items, matches.Count, pagination.PageNumber, pagination.PageSize));
        }
    }

    public Task<StockItemResponse?> GetStockItemAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(FindStock(warehouseCode, productSku)?.ToResponse());
        }
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

        // The key check, the change and the stored result run as one step under the store's lock
        lock (_store.SyncRoot)
        {
            if (_idempotencyContext.TryGetEntry(idempotencyKey, out var existingEntry))
            {
                // A retry gets the stored result; the same key with another payload is a client error
                return Task.FromResult(existingEntry.RequestHash == requestHash
                    ? new StockAdjustmentResult(true, (StockItemResponse)existingEntry.Response, null, existingEntry.StatusCode)
                    : new StockAdjustmentResult(false, null, "Idempotency key reused with a different request payload", 422));
            }

            var item = FindStock(warehouseCode, productSku);
            if (item is null)
            {
                return Task.FromResult(new StockAdjustmentResult(false, null, "Stock item not found", 404));
            }

            if (item.OnHand + quantityChange < item.Reserved)
            {
                return Task.FromResult(new StockAdjustmentResult(
                    false, null, "Adjustment would leave fewer units on hand than are reserved", 409));
            }

            item.OnHand += quantityChange;
            RecordHistory(item, reasonCode, quantityChange);

            // The adjustment changed a stock item that already existed: 200 with its new state
            var response = item.ToResponse();
            _idempotencyContext.Store(idempotencyKey, response, 200, requestHash);

            return Task.FromResult(new StockAdjustmentResult(true, response, null, 200));
        }
    }

    public Task<StockChangeOutcome> UpdateStockAsync(string warehouseCode, string productSku, int onHand, int reserved, int reorderThreshold, CancellationToken cancellationToken = default)
    {
        // The PUT endpoint answers this case with 400 first; the guard stops any other caller
        ArgumentOutOfRangeException.ThrowIfLessThan(onHand, reserved);

        lock (_store.SyncRoot)
        {
            var item = FindStock(warehouseCode, productSku);
            if (item is null)
            {
                return Task.FromResult(StockChangeOutcome.NotFound);
            }

            // Reserved units belong to the order workflows: a replacement may repeat them, never change them
            if (reserved != item.Reserved)
            {
                return Task.FromResult(StockChangeOutcome.ReservationConflict);
            }

            item.OnHand = onHand;
            item.ReorderThreshold = reorderThreshold;
            return Task.FromResult(StockChangeOutcome.Applied);
        }
    }

    public Task<StockChangeOutcome> DeleteStockAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var item = FindStock(warehouseCode, productSku);
            if (item is null)
            {
                return Task.FromResult(StockChangeOutcome.NotFound);
            }

            // Deleting reserved units would take stock away from the orders that hold it
            if (item.Reserved > 0)
            {
                return Task.FromResult(StockChangeOutcome.ReservationConflict);
            }

            // The history is the audit trail; its entries must keep naming an existing stock item
            if (_store.StockAdjustments.Any(a => a.StockItemId == item.Id))
            {
                return Task.FromResult(StockChangeOutcome.HasHistory);
            }

            _store.StockItems.Remove(Key(warehouseCode, productSku));
            return Task.FromResult(StockChangeOutcome.Applied);
        }
    }

    public Task ReserveStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var item = FindStock(warehouseCode, productSku);
            var available = item is null ? 0 : item.OnHand - item.Reserved;

            if (item is null || available < quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock of {productSku} in {warehouseCode}: {available} available, {quantity} requested.");
            }

            item.Reserved += quantity;
            return Task.CompletedTask;
        }
    }

    public Task ReleaseStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var item = FindReservedStock(warehouseCode, productSku, quantity);
            item.Reserved -= quantity;
            return Task.CompletedTask;
        }
    }

    public Task ShipStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            // Shipped units leave the warehouse: they stop being reserved and stop being on hand
            var item = FindReservedStock(warehouseCode, productSku, quantity);
            item.Reserved -= quantity;
            item.OnHand -= quantity;
            RecordHistory(item, "ORDER_SHIPMENT", -quantity);
            return Task.CompletedTask;
        }
    }

    private StockItem? FindStock(string warehouseCode, string productSku) =>
        _store.StockItems.GetValueOrDefault(Key(warehouseCode, productSku));

    // Warehouse codes compare without regard to case (Chapter 5), so the keys hold them in uppercase
    private static (string WarehouseCode, string ProductSku) Key(string warehouseCode, string productSku) =>
        (warehouseCode.ToUpperInvariant(), productSku);

    private StockItem FindReservedStock(string warehouseCode, string productSku, int quantity)
    {
        var item = FindStock(warehouseCode, productSku);

        if (item is null || item.Reserved < quantity)
        {
            throw new InvalidOperationException(
                $"Fewer than {quantity} units of {productSku} are reserved in {warehouseCode}.");
        }

        return item;
    }

    private void RecordHistory(StockItem item, string reasonCode, int quantityChange) =>
        _store.StockAdjustments.Add(new StockAdjustment
        {
            Id = Guid.NewGuid(),
            StockItemId = item.Id,
            ReasonCode = reasonCode,
            QuantityChange = quantityChange,
            AdjustedAt = DateTime.UtcNow
        });
}
