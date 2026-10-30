namespace WarehouseApi.Services;

public class InventoryService : IInventoryService
{
    private readonly IWarehouseRepository _repository;
    private readonly InventoryRequestLog _requestLog;

    public InventoryService(IWarehouseRepository repository, InventoryRequestLog requestLog)
    {
        _repository = repository;
        _requestLog = requestLog;
    }

    public Task<int?> GetStockQuantityAsync(string sku, string warehouseCode)
    {
        _requestLog.RecordLookup();
        return _repository.GetOnHandQuantityAsync(sku, warehouseCode);
    }
}
