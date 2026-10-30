namespace WarehouseApi.Contracts;

// One row of stock history with the warehouse and product it belongs to
public record StockAdjustmentAuditItem(
    Guid Id,
    string WarehouseCode,
    string ProductSku,
    string ReasonCode,
    int QuantityChange,
    int ReservedChange,
    string? ReferenceNumber,
    string? CorrelationId,
    DateTime AdjustedAt);
