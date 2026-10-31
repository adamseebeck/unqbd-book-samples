using Microsoft.EntityFrameworkCore;

public static class Chapter13Activity
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();
        await RetailDatabase.ResetAsync(db, Seed.CatalogAsync);

        var penId = await db.Products
            .Where(p => p.Sku == "PEN-003")
            .Select(p => p.Id)
            .SingleAsync();
        for (var sale = 1; sale <= 2; sale++)
        {
            var sold = await SellAsync(db, penId, 3);
            var stock = await StockAsync(db, penId);
            Console.WriteLine($"Sale {sale}: changed {sold}, " +
                $"Gel Pen stock {stock}");
        }

        var pad = await db.Products
            .SingleAsync(p => p.Sku == "PAD-002");
        await db.Products
            .Where(p => p.StockQuantity < 6)
            .ExecuteUpdateAsync(set => set.SetProperty(
                p => p.StockQuantity, p => p.StockQuantity + 10));
        var tracked = pad.StockQuantity;
        Console.WriteLine($"Tracked Desk Pad: {tracked}");
        db.ChangeTracker.Clear();
        pad = await db.Products
            .SingleAsync(p => p.Sku == "PAD-002");
        Console.WriteLine($"After Clear: {pad.StockQuantity}");

        await CompareAsync(db, "Projection", () => db.Products
            .Select(p => new { p.Id, p.Name }).ToListAsync());
        await CompareAsync(db, "Tracked",
            () => db.Products.ToListAsync());
        await CompareAsync(db, "No tracking",
            () => db.Products.AsNoTracking().ToListAsync());
    }

    private static Task<int> SellAsync(
        InventoryDbContext db, int id, int quantity) =>
        db.Products
            .Where(p => p.Id == id && p.StockQuantity >= quantity)
            .ExecuteUpdateAsync(set => set.SetProperty(
                p => p.StockQuantity,
                p => p.StockQuantity - quantity));

    private static Task<int> StockAsync(
        InventoryDbContext db, int id) =>
        db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => p.StockQuantity)
            .SingleAsync();

    private static async Task CompareAsync<T>(
        InventoryDbContext db, string name, Func<Task<T>> read)
    {
        db.ChangeTracker.Clear();
        var start = CommandLog.Count;
        await read();
        var commands = CommandLog.Count - start;
        var entries = db.ChangeTracker.Entries().Count();
        Console.WriteLine(
            $"{name}: {commands} command, {entries} entries");
    }
}
