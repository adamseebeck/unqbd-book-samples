using WarehouseApi.Models;

namespace WarehouseApi.Services;

public interface ILowStockAlertService
{
    Task<IReadOnlyList<StockAlert>> GetLowStockAlertsAsync(int threshold);
}
