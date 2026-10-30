namespace WarehouseApi.Contracts;

public record OrderResponse(
    Guid Id,
    string CustomerReference,
    OrderStatus Status,
    DateTime CreatedDate,
    string? FulfillmentWarehouseCode,
    IReadOnlyList<OrderLineResponse> Lines
);
