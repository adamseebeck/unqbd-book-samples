using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

// Bound with [AsParameters]; the defaults are constructor parameter defaults, as in Chapter 6
public record StockQueryFilter(
    string? Sku,
    string? WarehouseCode,
    bool? BelowReorderThreshold,
    [Range(1, 100_000)] int PageNumber = 1,
    [Range(1, 100)] int PageSize = 50);
