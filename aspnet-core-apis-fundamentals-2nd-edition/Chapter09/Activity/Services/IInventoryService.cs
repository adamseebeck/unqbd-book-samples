using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IInventoryService
{
    Task<PagedResult<StockItemResponse>> GetStockAsync(PaginationParameters pagination, StockFilterParameters filters, CancellationToken cancellationToken = default);
    Task<StockItemResponse?> GetStockItemAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default);
    Task<StockAdjustmentResult> AdjustStockAsync(string warehouseCode, string productSku, int quantityChange, string reasonCode, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<StockChangeOutcome> UpdateStockAsync(string warehouseCode, string productSku, int onHand, int reserved, int reorderThreshold, CancellationToken cancellationToken = default);
    Task<StockChangeOutcome> DeleteStockAsync(string warehouseCode, string productSku, CancellationToken cancellationToken = default);

    // Used by the order workflows; each call changes one stock item
    Task ReserveStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default);
    Task ReleaseStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default);
    Task ShipStockAsync(string warehouseCode, string productSku, int quantity, CancellationToken cancellationToken = default);
}
