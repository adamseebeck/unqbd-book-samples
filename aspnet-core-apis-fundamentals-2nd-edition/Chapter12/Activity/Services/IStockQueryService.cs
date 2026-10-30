using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IStockQueryService
{
    Task<PagedResult<StockListItem>> QueryAsync(StockQueryFilter filter, CancellationToken cancellationToken = default);
}
