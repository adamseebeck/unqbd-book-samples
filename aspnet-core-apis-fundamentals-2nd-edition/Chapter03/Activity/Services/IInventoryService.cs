namespace WarehouseApi.Services;

public interface IInventoryService
{
    Task<int?> GetStockQuantityAsync(string sku, string warehouseCode);
}
