using WarehouseApi.Contracts;
using WarehouseApi.Models;

namespace WarehouseApi.Mappings;

public static class EntityMappings
{
    public static ProductResponse ToResponse(this Product product) =>
        new(product.Sku, product.Name, product.Description, product.UnitPrice, product.IsActive);

    public static WarehouseResponse ToResponse(this Warehouse warehouse) =>
        new(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Location, warehouse.IsOperational);

    public static StockItemResponse ToResponse(this StockItem item) =>
        new(item.ProductSku,
            item.WarehouseCode,
            item.OnHand,
            item.Reserved,
            item.OnHand - item.Reserved,
            item.ReorderThreshold);

    public static OrderResponse ToResponse(this Order order) =>
        new(order.Id,
            order.CustomerReference,
            order.Status,
            order.CreatedDate,
            order.FulfillmentWarehouseCode,
            order.Lines.Select(l => new OrderLineResponse(
                l.Sku, l.RequestedQuantity, l.ReservedQuantity, l.ShippedQuantity)).ToList());
}
