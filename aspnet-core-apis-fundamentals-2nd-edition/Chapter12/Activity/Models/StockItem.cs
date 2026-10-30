namespace WarehouseApi.Models;

public class StockItem
{
    public Guid Id { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int ReorderThreshold { get; set; }

    // Set by SQL Server on every update; EF Core uses it to detect lost updates
    public byte[] RowVersion { get; set; } = [];

    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
