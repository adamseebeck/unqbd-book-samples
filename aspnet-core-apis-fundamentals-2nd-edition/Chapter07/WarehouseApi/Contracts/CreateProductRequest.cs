namespace WarehouseApi.Contracts;

public record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice);
