namespace WarehouseApi.Contracts;

public record OrderLineResponse(
    string Sku,
    int RequestedQuantity,
    int ReservedQuantity,
    int ShippedQuantity
);
