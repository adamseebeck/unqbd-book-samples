namespace WarehouseApi.Contracts;

public record CreateStockItemRequest(
    string ProductSku,
    int OnHand,
    int Reserved,
    int ReorderThreshold);
