using System.Collections.Concurrent;
using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public class StockService : IStockService
{
    private readonly ConcurrentDictionary<int, StockAdjustmentResponse> _adjustments = new();
    private int _lastId;

    public Task<StockAdjustmentResponse> CreateAdjustmentAsync(string warehouseCode, CreateStockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        // Interlocked.Increment gives every concurrent request its own ID
        var id = Interlocked.Increment(ref _lastId);

        var adjustment = new StockAdjustmentResponse(
            id,
            request.ProductSku,
            warehouseCode.ToUpperInvariant(),
            request.QuantityChange,
            request.ReasonCode!.Value, // the endpoint has rejected a missing reason
            DateTimeOffset.UtcNow);

        _adjustments[id] = adjustment;
        return Task.FromResult(adjustment);
    }

    public Task<StockAdjustmentResponse?> GetAdjustmentAsync(string warehouseCode, int id, CancellationToken cancellationToken = default)
    {
        var found = _adjustments.TryGetValue(id, out var adjustment) && SameWarehouse(adjustment.WarehouseCode, warehouseCode);
        return Task.FromResult(found ? adjustment : null);
    }

    public Task<IReadOnlyList<StockAdjustmentResponse>> GetAdjustmentsAsync(string warehouseCode, StockAdjustmentReason? reasonCode, DateTimeOffset? dateFrom, CancellationToken cancellationToken = default)
    {
        var query = _adjustments.Values.Where(a => SameWarehouse(a.WarehouseCode, warehouseCode));

        if (reasonCode.HasValue)
        {
            query = query.Where(a => a.ReasonCode == reasonCode.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= dateFrom.Value);
        }

        return Task.FromResult<IReadOnlyList<StockAdjustmentResponse>>(query.OrderBy(a => a.Id).ToList());
    }

    // Warehouse codes compare without regard to case; the log stores them in uppercase
    private static bool SameWarehouse(string stored, string requested) =>
        string.Equals(stored, requested, StringComparison.OrdinalIgnoreCase);
}
