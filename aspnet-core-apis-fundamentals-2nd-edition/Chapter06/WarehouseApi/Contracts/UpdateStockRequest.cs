namespace WarehouseApi.Contracts;

public record UpdateStockRequest(
    int OnHand,
    int Reserved,
    int ReorderThreshold);
