namespace WarehouseApi.Services;

public interface IStockHistoryService
{
    Task<StockAdjustmentDetails?> GetAdjustmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockAdjustmentDetails>> GetRecentAsync(string warehouseCode, int count, CancellationToken cancellationToken = default);
}
