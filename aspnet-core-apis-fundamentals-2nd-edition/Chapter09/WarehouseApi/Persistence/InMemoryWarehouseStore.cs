using WarehouseApi.Models;

namespace WarehouseApi.Persistence;

// Registered as a Singleton: every request reads and writes these collections
public class InMemoryWarehouseStore
{
    // One lock guards every collection; services hold it for each read and each write
    public Lock SyncRoot { get; } = new();

    // Lets one order workflow run at a time, because a workflow awaits several service calls
    public SemaphoreSlim OrderWorkflowGate { get; } = new(1, 1);

    public Dictionary<string, Product> Products { get; } = new();
    // Warehouse codes compare without regard to case (Chapter 5)
    public Dictionary<string, Warehouse> Warehouses { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<(string WarehouseCode, string ProductSku), StockItem> StockItems { get; } = new();
    public Dictionary<Guid, Order> Orders { get; } = new();
    public List<StockAdjustment> StockAdjustments { get; } = new();

    public InMemoryWarehouseStore()
    {
        // The sample data from Chapters 5 and 6, without reserved units: only an allocation reserves stock
        AddProduct("SKU-001", "Industrial Shelf Bracket", "Steel bracket for pallet racking", 12.99m);
        AddProduct("SKU-002", "Heavy Duty Casters (Set of 4)", "Swivel casters for warehouse carts", 45.50m);

        AddWarehouse(1, "WH-001", "North Distribution Center", "Chicago, IL");
        AddWarehouse(2, "WH-002", "South Fulfillment Hub", "Atlanta, GA");

        AddStock("WH-001", "SKU-001", onHand: 150, reorderThreshold: 40);
        AddStock("WH-001", "SKU-002", onHand: 75, reorderThreshold: 100);
        AddStock("WH-002", "SKU-001", onHand: 60, reorderThreshold: 25);
    }

    private void AddProduct(string sku, string name, string description, decimal unitPrice) =>
        Products[sku] = new Product
        {
            Sku = sku,
            Name = name,
            Description = description,
            UnitPrice = unitPrice,
            IsActive = true
        };

    private void AddWarehouse(int id, string code, string name, string location) =>
        Warehouses[code] = new Warehouse
        {
            Id = id,
            Code = code,
            Name = name,
            Location = location,
            IsOperational = true
        };

    private void AddStock(string warehouseCode, string productSku, int onHand, int reorderThreshold) =>
        StockItems[(warehouseCode, productSku)] = new StockItem
        {
            Id = Guid.NewGuid(),
            WarehouseCode = warehouseCode,
            ProductSku = productSku,
            OnHand = onHand,
            ReorderThreshold = reorderThreshold
        };
}
