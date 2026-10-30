namespace WarehouseApi.Contracts;

public record ProductSummary(
    string Sku,
    string Name,
    decimal UnitPrice,
    bool IsActive
);
