namespace WarehouseApi.Models;

public class StockItem
{
    public Guid Id { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int ReorderThreshold { get; set; }
}
