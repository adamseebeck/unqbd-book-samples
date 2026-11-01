using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public sealed record LowStockRow(
    int Id, string Name, string Category, int StockQuantity);

public static class LowStockReport
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();
        await RetailDatabase.ResetAsync(db, SeedAsync);

        const int threshold = 20, pageSize = 2;
        var offset = new List<LowStockRow>();
        for (var page = 1; ; page++)
        {
            var rows = await Query(db, threshold, pageSize)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new LowStockRow(p.Id, p.Name,
                    p.Category.Name, p.StockQuantity))
                .ToListAsync();
            if (rows.Count == 0) break;
            foreach (var r in rows)
                Console.WriteLine(
                    $"Offset page {page}: {r.Name} " +
                    $"({r.Category}, {r.StockQuantity})");
            offset.AddRange(rows);
        }

        var seek = new List<LowStockRow>();
        var lastStock = -1;
        var lastId = 0;
        for (var page = 1; ; page++)
        {
            var rows = await Query(db, threshold, pageSize)
                .Where(p => p.StockQuantity > lastStock
                    || (p.StockQuantity == lastStock
                        && p.Id > lastId))
                .Take(pageSize)
                .Select(p => new LowStockRow(p.Id, p.Name,
                    p.Category.Name, p.StockQuantity))
                .ToListAsync();
            if (rows.Count == 0) break;
            foreach (var r in rows)
                Console.WriteLine(
                    $"Seek page {page}: {r.Name} " +
                    $"({r.Category}, {r.StockQuantity})");
            seek.AddRange(rows);
            lastStock = rows[^1].StockQuantity;
            lastId = rows[^1].Id;
        }

        var same = offset.Select(r => r.Id)
            .SequenceEqual(seek.Select(r => r.Id));
        Console.WriteLine($"Same rows: {same}");

        try { Query(db, -1, pageSize); }
        catch (ArgumentOutOfRangeException)
        { Console.WriteLine("Rejected threshold -1"); }
        try { Query(db, threshold, 0); }
        catch (ArgumentOutOfRangeException)
        { Console.WriteLine("Rejected page size 0"); }
    }

    public static IQueryable<Product> Query(
        InventoryDbContext db, int threshold, int pageSize)
    {
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(
                nameof(threshold));
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize));
        return db.Products
            .Where(p => p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity).ThenBy(p => p.Id);
    }

    private static async Task SeedAsync(InventoryDbContext db)
    {
        await Seed.CatalogAsync(db);
        var office = await db.Categories
            .SingleAsync(c => c.Name == "Office");
        db.Products.AddRange(
            new Product
            {
                Sku = "NOTE-004", Name = "Sticky Notes",
                Price = 3.00m, StockQuantity = 2,
                Category = office
            },
            new Product
            {
                Sku = "MARK-005", Name = "Marker",
                Price = 1.75m, StockQuantity = 5,
                Category = office
            });
        await db.SaveChangesAsync();
    }
}
