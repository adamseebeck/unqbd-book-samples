namespace MauiInventory.Core;
public sealed class DemoPageSource : IPageSource
{
    public bool FailNext { get; set; }

    public async Task<IReadOnlyList<Product>> ReadAsync(int page, int pageSize, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        await Task.Delay(500, token);
        if (FailNext)
        {
            FailNext = false;
            throw new HttpRequestException("Controlled paging failure");
        }

        if (page >= 3)
            return[];
        return Enumerable.Range(page * pageSize + 1, page == 2 ? 5 : pageSize).Select(i => new Product { Id = i, Sku = $"P{i}", Name = $"Product {i}", Quantity = (i % 4) switch
        {
            0 => 9,
            1 => 10,
            2 => 50,
            _ => 51
        }, UnitPriceCents = 100 }).ToArray();
    }
}
