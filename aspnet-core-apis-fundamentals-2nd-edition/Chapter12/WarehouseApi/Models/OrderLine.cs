namespace WarehouseApi.Models;

public class OrderLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int ShippedQuantity { get; set; }
}
