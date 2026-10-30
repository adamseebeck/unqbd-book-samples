namespace WarehouseApi.Contracts;

public record ProductSearchParameters(
    string? SearchTerm,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 20);
