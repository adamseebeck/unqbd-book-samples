using Microsoft.EntityFrameworkCore;

public static class Chapter13Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();
        await RetailDatabase.ResetAsync(db, Seed.CatalogAsync);

        var lowStock = InventoryQueries.LowStock(db, 6);
        await foreach (var row in lowStock)
            Console.WriteLine($"Compiled query: {row.Name}");

        var graph = db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductCategories);
        var start = CommandLog.Count;
        await graph.AsSplitQuery().ToListAsync();
        var split = CommandLog.Count - start;
        start = CommandLog.Count;
        await graph.AsSingleQuery().ToListAsync();
        var single = CommandLog.Count - start;
        Console.WriteLine($"Split query: {split} commands");
        Console.WriteLine($"Single query: {single} command");

        await db.Products.ToListAsync();
        var tracked = db.ChangeTracker.Entries().Count();
        db.ChangeTracker.Clear();
        await db.Products.AsNoTracking().ToListAsync();
        var untracked = db.ChangeTracker.Entries().Count();
        Console.WriteLine($"Tracked read: {tracked} entries");
        Console.WriteLine($"No tracking: {untracked} entries");

        var updated = await db.Products
            .Where(p => p.StockQuantity < 6)
            .ExecuteUpdateAsync(set => set.SetProperty(
                p => p.StockQuantity, p => p.StockQuantity + 10));
        var stock = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => p.StockQuantity)
            .ToListAsync();
        Console.WriteLine($"Bulk update: {updated} rows, " +
            $"stock now {string.Join(", ", stock)}");
    }
}
