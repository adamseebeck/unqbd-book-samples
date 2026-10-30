namespace WarehouseApi.Services;

public class FixedWarehouseService
{
    private readonly IWarehouseRepository _repository;

    public FixedWarehouseService(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public Task<int?> QueryStockAsync(string sku, string warehouseCode)
    {
        return _repository.GetOnHandQuantityAsync(sku, warehouseCode);
    }
}
