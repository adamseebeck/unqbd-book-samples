namespace WarehouseApi.Contracts;

public record StockAdjustmentRequest(
    string Sku,
    string WarehouseCode,
    int Quantity,
    string ReasonCode
);
