using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IInventoryService
{
    Task<PagedResult<StockItemResponse>> GetStockAsync(PaginationParameters pagination, StockFilterParameters filters, CancellationToken cancellationToken = default);
    Task<StockItemResponse?> GetStockItemAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default);
    Task<StockAdjustmentResult> AdjustStockAsync(string warehouseCode, string productSku, int quantityChange, string reasonCode, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<bool> UpdateStockAsync(string warehouseCode, string productSku, int onHand, int reserved, int reorderThreshold, CancellationToken cancellationToken = default);
    Task<bool> DeleteStockAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default);
}
