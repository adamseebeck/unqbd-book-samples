namespace WarehouseApi.Services;

// What the service knows about one stock adjustment; each API version picks the fields it exposes
public record StockAdjustmentDetails(
    Guid Id,
    string WarehouseCode,
    string ProductSku,
    string ReasonCode,
    int QuantityChange,
    int ReservedChange,
    string? ReferenceNumber,
    string? CorrelationId,
    DateTime AdjustedAt);
