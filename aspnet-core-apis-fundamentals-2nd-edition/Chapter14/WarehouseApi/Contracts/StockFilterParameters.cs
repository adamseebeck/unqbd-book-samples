namespace WarehouseApi.Contracts;

public record StockFilterParameters(
    string? WarehouseCode,
    string? ProductSku,
    int? MinQuantity,
    int? MaxQuantity,
    string? SortBy = "ProductSku",
    bool SortDescending = false)
{
    public static readonly string[] SortKeys = ["ProductSku", "OnHand", "Available"];

    // Sort keys compare without regard to case, like the query string keys they arrive with
    public bool HasKnownSortKey => SortBy is null || SortKeys.Contains(SortBy, StringComparer.OrdinalIgnoreCase);
}
