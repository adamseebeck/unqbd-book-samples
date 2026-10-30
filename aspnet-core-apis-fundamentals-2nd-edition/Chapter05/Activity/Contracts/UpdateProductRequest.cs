namespace WarehouseApi.Contracts;

// IsActive is nullable so the handler can reject a PUT that omits it; a plain bool would bind
// false and take the product off sale
public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal UnitPrice,
    bool? IsActive);
