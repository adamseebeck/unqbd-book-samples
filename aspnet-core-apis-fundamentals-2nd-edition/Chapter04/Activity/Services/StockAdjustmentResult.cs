using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public enum StockAdjustmentStatus
{
    Applied,
    StockItemNotFound,
    InsufficientStock
}

public record StockAdjustmentResult(StockAdjustmentStatus Status, StockLevelResponse? StockLevel = null);
