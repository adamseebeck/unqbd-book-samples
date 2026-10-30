using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Models;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class EfInventoryService : IInventoryService
{
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);
    private readonly WarehouseDbContext _context;

    public EfInventoryService(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StockItemResponse>> GetStockAsync(PaginationParameters pagination, StockFilterParameters filters, CancellationToken cancellationToken = default)
    {
        var query = _context.StockItems.AsNoTracking();

        if (!string.IsNullOrEmpty(filters.WarehouseCode))
            query = query.Where(s => s.WarehouseCode == filters.WarehouseCode);

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

        // The SKU breaks ties, so items with equal values keep their order from page to page
        var ordered = sorted.ThenBy(s => s.ProductSku);

        // The filters, the sort and the page run in SQL; the count is a second query
        var totalCount = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockItemResponse>(
            items.Select(s => s.ToResponse()).ToList(), totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<StockItemResponse?> GetStockItemAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default)
    {
        var item = await _context.StockItems
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WarehouseCode == warehouseCode && s.ProductSku == productSku, cancellationToken);

        return item?.ToResponse();
    }

    public async Task<StockAdjustmentResult> AdjustStockAsync(string warehouseCode, string productSku, int quantityChange, string reasonCode, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (quantityChange == 0)
        {
            return new StockAdjustmentResult(false, null, "An adjustment must change the quantity", 400);
        }

        // Warehouse codes compare without regard to case, so a retry that writes wh-001 is the same request
        var requestHash = CreateRequestHash($"adjust|{warehouseCode.ToUpperInvariant()}|{productSku}|{quantityChange}|{reasonCode}");

        var record = await _context.IdempotencyRecords.FindAsync([idempotencyKey], cancellationToken);
        if (record is not null && IsCurrent(record))
        {
            return Replay(record, requestHash);
        }

        var item = await FindTrackedAsync(warehouseCode, productSku, cancellationToken);
        if (item is null)
        {
            return new StockAdjustmentResult(false, null, "Stock item not found", 404);
        }

        if (item.OnHand + quantityChange < item.Reserved)
        {
            return new StockAdjustmentResult(
                false, null, "Adjustment would leave fewer units on hand than are reserved", 409);
        }

        item.OnHand += quantityChange;
        RecordHistory(item, reasonCode, quantityChange);

        // A new key gets a row; an expired key's row is reused
        var response = item.ToResponse();
        record ??= _context.IdempotencyRecords.Add(new IdempotencyRecord { Key = idempotencyKey }).Entity;
        record.RequestHash = requestHash;
        record.StatusCode = 200;
        record.ResponseBody = JsonSerializer.Serialize(response);
        record.CreatedAt = DateTime.UtcNow;

        try
        {
            // One SaveChangesAsync call: the stock change, its history row and the stored result commit together
            await _context.SaveChangesAsync(cancellationToken);
            return new StockAdjustmentResult(true, response, null, 200);
        }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException || SqlServerErrors.IsDuplicateKey(ex))
        {
            // Another request changed the stock item or stored the key first; nothing from this attempt was saved
            _context.ChangeTracker.Clear();

            var winner = await _context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == idempotencyKey, cancellationToken);

            // Replay only a result stored in the last 24 hours, not an expired row this request tried to reuse
            return winner is not null && IsCurrent(winner)
                ? Replay(winner, requestHash)
                : new StockAdjustmentResult(false, null, "The stock item was changed by another request; read it again and retry", 409);
        }
    }

    public async Task<StockChangeOutcome> UpdateStockAsync(string warehouseCode, string productSku, int onHand, int reserved, int reorderThreshold, CancellationToken cancellationToken = default)
    {
        // The PUT endpoint answers this case with 400 first; the guard stops any other caller
        ArgumentOutOfRangeException.ThrowIfLessThan(onHand, reserved);

        // One UPDATE statement checks and replaces: it changes the row only while the stored Reserved
        // value equals the one in the request, because only the order workflows change reserved units
        var updated = await _context.StockItems
            .Where(s => s.WarehouseCode == warehouseCode && s.ProductSku == productSku && s.Reserved == reserved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.OnHand, onHand)
                .SetProperty(s => s.ReorderThreshold, reorderThreshold),
                cancellationToken);

        if (updated == 1)
        {
            return StockChangeOutcome.Applied;
        }

        return await ExistsAsync(warehouseCode, productSku, cancellationToken)
            ? StockChangeOutcome.ReservationConflict
            : StockChangeOutcome.NotFound;
    }

    public async Task<StockChangeOutcome> DeleteStockAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default)
    {
        // Only a stock item without reserved units and without history is deleted. The history is the
        // audit trail, and its foreign key (Restrict) refuses the delete of an item that has any
        var deleted = await _context.StockItems
            .Where(s => s.WarehouseCode == warehouseCode && s.ProductSku == productSku && s.Reserved == 0
                && !_context.StockAdjustments.Any(a => a.StockItemId == s.Id))
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 1)
        {
            return StockChangeOutcome.Applied;
        }

        // Nothing was deleted: tell a missing item from one with reserved units or with history
        var reserved = await _context.StockItems
            .Where(s => s.WarehouseCode == warehouseCode && s.ProductSku == productSku)
            .Select(s => (int?)s.Reserved)
            .FirstOrDefaultAsync(cancellationToken);

        return reserved switch
        {
            null => StockChangeOutcome.NotFound,
            > 0 => StockChangeOutcome.ReservationConflict,
            _ => StockChangeOutcome.HasHistory
        };
    }

    public async Task ReserveStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default)
    {
        var item = await FindTrackedAsync(warehouseCode, productSku, cancellationToken);
        var available = item is null ? 0 : item.OnHand - item.Reserved;

        if (item is null || available < quantity)
        {
            throw new InvalidOperationException(
                $"Insufficient stock of {productSku} in {warehouseCode}: {available} available, {quantity} requested.");
        }

        item.Reserved += quantity;

        // Throws DbUpdateConcurrencyException if another request changed the row after it was read
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default)
    {
        var item = await FindReservedAsync(warehouseCode, productSku, quantity, cancellationToken);
        item.Reserved -= quantity;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ShipStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default)
    {
        // Shipped units leave the warehouse: they stop being reserved and stop being on hand
        var item = await FindReservedAsync(warehouseCode, productSku, quantity, cancellationToken);
        item.Reserved -= quantity;
        item.OnHand -= quantity;
        RecordHistory(item, "ORDER_SHIPMENT", -quantity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Task<StockItem?> FindTrackedAsync(string warehouseCode, string productSku, CancellationToken cancellationToken) =>
        _context.StockItems.FirstOrDefaultAsync(
            s => s.WarehouseCode == warehouseCode && s.ProductSku == productSku, cancellationToken);

    private Task<bool> ExistsAsync(string warehouseCode, string productSku, CancellationToken cancellationToken) =>
        _context.StockItems.AnyAsync(
            s => s.WarehouseCode == warehouseCode && s.ProductSku == productSku, cancellationToken);

    private async Task<StockItem> FindReservedAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken)
    {
        var item = await FindTrackedAsync(warehouseCode, productSku, cancellationToken);

        if (item is null || item.Reserved < quantity)
        {
            throw new InvalidOperationException(
                $"Fewer than {quantity} units of {productSku} are reserved in {warehouseCode}.");
        }

        return item;
    }

    private void RecordHistory(StockItem item, string reasonCode, int quantityChange) =>
        _context.StockAdjustments.Add(new StockAdjustment
        {
            StockItemId = item.Id,
            ReasonCode = reasonCode,
            QuantityChange = quantityChange,
            AdjustedAt = DateTime.UtcNow
        });

    private static bool IsCurrent(IdempotencyRecord record) =>
        record.CreatedAt > DateTime.UtcNow - IdempotencyWindow;

    private static StockAdjustmentResult Replay(IdempotencyRecord record, string requestHash)
    {
        // Same key with a different payload is a client error, not a retry (422, as the Idempotency-Key draft recommends)
        if (record.RequestHash != requestHash)
        {
            return new StockAdjustmentResult(
                false, null, "Idempotency key reused with a different request payload", 422);
        }

        var response = JsonSerializer.Deserialize<StockItemResponse>(record.ResponseBody);
        return new StockAdjustmentResult(true, response, null, record.StatusCode);
    }

    private static string CreateRequestHash(string requestSummary) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(requestSummary)));
}
