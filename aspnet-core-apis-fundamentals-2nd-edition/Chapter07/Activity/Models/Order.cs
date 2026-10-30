using WarehouseApi.Contracts;

namespace WarehouseApi.Models;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? FulfillmentWarehouseCode { get; set; }
    public List<OrderLine> Lines { get; set; } = new();
}
