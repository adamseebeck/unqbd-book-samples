namespace WarehouseApi.Contracts;

public record StockAdjustmentRequest(
    int QuantityChange,
    string ReasonCode);
