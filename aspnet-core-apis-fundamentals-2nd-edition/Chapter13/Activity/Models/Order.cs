using WarehouseApi.Contracts;

namespace WarehouseApi.Models;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? FulfillmentWarehouseCode { get; set; }

    // The NameIdentifier claim of the client that created the order; integration clients read their own orders
    public string? CreatedBy { get; set; }
    public List<OrderLine> Lines { get; set; } = new();

    // Two requests that change the same order cannot both succeed
    public byte[] RowVersion { get; set; } = [];
}
