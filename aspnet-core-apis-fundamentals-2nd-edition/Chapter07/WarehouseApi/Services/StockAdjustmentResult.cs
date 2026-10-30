using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public record StockAdjustmentResult(
    bool Success,
    StockItemResponse? StockItem,
    string? ErrorMessage,
    int StatusCode = 200);
