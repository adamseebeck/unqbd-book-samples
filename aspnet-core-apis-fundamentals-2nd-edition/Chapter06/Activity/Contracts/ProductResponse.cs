namespace WarehouseApi.Contracts;

public record ProductResponse(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    bool IsActive);
