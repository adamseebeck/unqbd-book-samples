namespace WarehouseApi.Services;

public interface IWarehouseRepository
{
    Task<int?> GetOnHandQuantityAsync(string sku, string warehouseCode);
}
