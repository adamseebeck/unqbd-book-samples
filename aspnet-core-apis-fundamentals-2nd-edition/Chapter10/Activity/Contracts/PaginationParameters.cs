namespace WarehouseApi.Contracts;

public record PaginationParameters(int PageNumber = 1, int PageSize = 10)
{
    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;

    // The page number limit keeps Skip within the range of int
    public bool IsValid => PageNumber is >= 1 and <= 100_000 && PageSize is >= 1 and <= 100;
}
