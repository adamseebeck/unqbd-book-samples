using Microsoft.EntityFrameworkCore;

public static class Chapter14Activity
{
    public static async Task RunAsync(IServiceProvider services)
    {
        DateTime before;
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            await RetailDatabase.ResetAsync(
                db, Seed.CatalogAsync);
            var pen = await db.Products
                .SingleAsync(p => p.Sku == "PEN-003");
            pen.IsDeleted = true;
            await db.SaveChangesAsync();
            before = await StampAsync(db, "PEN-003");
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var pen = await db.Products.IgnoreQueryFilters()
                .SingleAsync(p => p.Sku == "PEN-003");
            if (!pen.IsDeleted)
                throw new InvalidOperationException(
                    "Gel Pen is not deleted.");
            pen.IsDeleted = false;
            db.SaveChanges();
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var visible = await db.Products.CountAsync();
            var later = await StampAsync(db, "PEN-003") > before;
            Console.WriteLine(
                $"Visible after restore: {visible}");
            Console.WriteLine($"Restore stamped: {later}");

            var now = DateTime.UtcNow;
            var retired = await db.Products
                .Where(p => p.StockQuantity < 3)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => EF.Property<DateTime>(
                        p, "LastModifiedUtc"), now));
            visible = await db.Products.CountAsync();
            var same = await StampAsync(db, "PAD-002") == now;
            Console.WriteLine($"Bulk retired: {retired}");
            Console.WriteLine($"Visible after retire: {visible}");
            Console.WriteLine($"Bulk stamp set: {same}");
        }
    }

    private static Task<DateTime> StampAsync(
        InventoryDbContext db, string sku) =>
        db.Products.IgnoreQueryFilters()
            .Where(p => p.Sku == sku)
            .Select(p => EF.Property<DateTime>(
                p, "LastModifiedUtc"))
            .SingleAsync();
}
