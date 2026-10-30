namespace WarehouseApi.Contracts;

public record StockAdjustmentResponse(
    int Id,
    string ProductSku,
    string WarehouseCode,
    int QuantityChange,
    StockAdjustmentReason ReasonCode,
    DateTimeOffset CreatedAt);
