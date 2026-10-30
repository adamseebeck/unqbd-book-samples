namespace WarehouseApi.Services;

// Anti-pattern demonstration: do not register this class as a Singleton
public class CaptiveDependencyDemoService
{
    private readonly IInventoryService _inventoryService;

    public CaptiveDependencyDemoService(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<int?> CheckStockAsync(string sku, string warehouseCode)
    {
        return _inventoryService.GetStockQuantityAsync(sku, warehouseCode);
    }
}
