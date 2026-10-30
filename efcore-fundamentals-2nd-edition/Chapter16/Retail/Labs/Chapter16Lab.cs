using Microsoft.EntityFrameworkCore;

public static class Chapter16Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();
        await RetailDatabase.ResetAsync(db, Seed.CatalogAsync);
        var audit = services.GetRequiredService<ReaderAudit>();
        audit.Reset();

        var rows = await db.Products
            .TagWith("Inventory.LowStock.v1")
            .AsNoTracking()
            .Where(p => p.StockQuantity < 6)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        var ms = audit.LastDuration.TotalMilliseconds;
        Console.WriteLine($"Rows: {rows.Count}");
        Console.WriteLine($"Reader commands: {audit.Count}");
        Console.WriteLine($"Tag line: {audit.LastFirstLine}");
        Console.WriteLine($"Execution time: {ms:F1} ms");
    }
}
