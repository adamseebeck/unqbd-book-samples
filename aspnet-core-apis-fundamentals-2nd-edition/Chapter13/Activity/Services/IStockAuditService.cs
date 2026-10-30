using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IStockAuditService
{
    Task<PagedResult<StockAdjustmentAuditItem>> GetHistoryAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);
}
