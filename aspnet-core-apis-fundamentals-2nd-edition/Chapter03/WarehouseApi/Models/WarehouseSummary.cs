namespace WarehouseApi.Models;

public record WarehouseSummary(
    string Code,
    string Name,
    string Location,
    bool IsOperational,
    int TotalStockItems
);
