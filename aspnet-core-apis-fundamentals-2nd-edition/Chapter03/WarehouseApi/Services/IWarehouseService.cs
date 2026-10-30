using WarehouseApi.Models;

namespace WarehouseApi.Services;

public interface IWarehouseService
{
    Task<WarehouseSummary?> GetSummaryAsync(string warehouseCode);
}
