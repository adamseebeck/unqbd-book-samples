using Microsoft.Extensions.DependencyInjection;

public static class Chapter09Activity
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
            await RetailDatabase.ResetAsync(
                scope.GetContext(), Seed.CatalogAsync);

        using var request = services.CreateScope();
        var search = request.ServiceProvider
            .GetRequiredService<IProductSearch>();
        await PrintAsync("Category 1", search, 1, null);
        await PrintAsync("Below 6", search, null, 6);
        await PrintAsync("Category 1, below 6", search, 1, 6);
        await PrintAsync("Category 2, below 3", search, 2, 3);
        await PrintAsync("Category 1, page 2", search, 1, null,
            page: 2, pageSize: 1);
        await PrintAsync("Stub", new StubProductSearch(),
            null, null);
    }

    private static async Task PrintAsync(string label,
        IProductSearch search, int? categoryId, int? threshold,
        int page = 1, int pageSize = 20)
    {
        var rows = await search.SearchAsync(
            categoryId, threshold, page, pageSize);
        var text = rows.Count == 0 ? "(none)" : string.Join(", ",
            rows.Select(r => $"{r.Name} " +
                $"({r.CategoryName}, {r.StockQuantity})"));
        Console.WriteLine($"{label}: {text}");
    }
}
