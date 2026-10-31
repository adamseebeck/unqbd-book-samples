using Microsoft.EntityFrameworkCore;

public static class Chapter16Activity
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();
        await RetailDatabase.ResetAsync(db, Seed.CatalogAsync);
        var audit = services.GetRequiredService<ReaderAudit>();
        audit.Reset();

        await db.Products
            .TagWith("Inventory.LowStock.v1")
            .AsNoTracking()
            .Where(p => p.StockQuantity < 6)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();
        await db.Products
            .Where(p => p.StockQuantity < 6)
            .ExecuteUpdateAsync(set => set.SetProperty(
                p => p.StockQuantity, p => p.StockQuantity + 10));
        Console.WriteLine($"Reader commands: {audit.Count}");
        Console.WriteLine(
            $"Non-query commands: {audit.NonQueryCount}");

        var limit = TimeSpan.FromMilliseconds(10);
        var nine = TimeSpan.FromMilliseconds(9);
        var below = ReaderAudit.IsSlow(nine, limit);
        var at = ReaderAudit.IsSlow(limit, limit);
        Console.WriteLine($"IsSlow(9 ms, 10 ms): {below}");
        Console.WriteLine($"IsSlow(10 ms, 10 ms): {at}");

        var records = audit.Records();
        Console.WriteLine($"Records kept: {records.Length}");
        foreach (var record in records)
            Console.WriteLine(
                $"{record.Kind}: {record.FirstLine}");
    }
}
