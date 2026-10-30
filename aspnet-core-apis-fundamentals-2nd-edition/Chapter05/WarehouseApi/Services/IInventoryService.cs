using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IInventoryService
{
    Task<StockLevelResponse?> GetStockLevelAsync(string warehouseCode, string sku, CancellationToken cancellationToken = default);
    Task<StockAdjustmentResult> AdjustStockAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default);
}
