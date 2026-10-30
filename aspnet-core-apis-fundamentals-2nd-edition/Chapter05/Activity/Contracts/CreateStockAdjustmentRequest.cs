namespace WarehouseApi.Contracts;

// The route supplies the warehouse code, so the body does not repeat it. ReasonCode is nullable:
// a non-nullable enum would bind a missing value as Receipt, the enum's first value
public record CreateStockAdjustmentRequest(
    string ProductSku,
    int QuantityChange,
    StockAdjustmentReason? ReasonCode);
