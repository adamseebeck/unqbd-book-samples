namespace WarehouseApi.Models;

// One row of stock history: which stock item changed, by how much, and why
public class StockAdjustment
{
    public Guid Id { get; set; }
    public Guid StockItemId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public DateTime AdjustedAt { get; set; }
}
