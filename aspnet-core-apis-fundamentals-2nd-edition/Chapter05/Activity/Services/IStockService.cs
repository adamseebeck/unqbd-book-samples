using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IStockService
{
    Task<StockAdjustmentResponse> CreateAdjustmentAsync(string warehouseCode, CreateStockAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<StockAdjustmentResponse?> GetAdjustmentAsync(string warehouseCode, int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockAdjustmentResponse>> GetAdjustmentsAsync(string warehouseCode, StockAdjustmentReason? reasonCode, DateTimeOffset? dateFrom, CancellationToken cancellationToken = default);
}
