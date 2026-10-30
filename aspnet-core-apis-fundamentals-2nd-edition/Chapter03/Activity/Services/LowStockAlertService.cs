using WarehouseApi.Models;

namespace WarehouseApi.Services;

public class LowStockAlertService : ILowStockAlertService
{
    private readonly IWarehouseRepository _repository;

    public LowStockAlertService(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<StockAlert>> GetLowStockAlertsAsync(int threshold)
    {
        var stock = await _repository.GetAllStockAsync();

        return stock
            .Where(level => level.Quantity < threshold)
            .OrderBy(level => level.Sku)
            .ThenBy(level => level.WarehouseCode)
            .Select(level => new StockAlert(level.Sku, level.WarehouseCode, level.Quantity))
            .ToList();
    }
}
