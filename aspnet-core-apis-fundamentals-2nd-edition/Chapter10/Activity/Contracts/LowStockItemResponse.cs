namespace WarehouseApi.Contracts;

public record LowStockItemResponse(
    string ProductSku,
    string ProductName,
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int ReorderThreshold);
