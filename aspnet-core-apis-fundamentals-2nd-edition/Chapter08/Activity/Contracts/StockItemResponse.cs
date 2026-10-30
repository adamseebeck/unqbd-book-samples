namespace WarehouseApi.Contracts;

public record StockItemResponse(
    string ProductSku,
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int Available,
    int ReorderThreshold);
