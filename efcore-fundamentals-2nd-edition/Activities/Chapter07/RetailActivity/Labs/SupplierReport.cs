using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class SupplierReport
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            await RetailDatabase.ResetAsync(db, SeedAsync);

            var report = await db.Suppliers
                .Include(s => s.Products)
                .OrderBy(s => s.CompanyName)
                .ToListAsync();
            foreach (var s in report)
                Console.WriteLine($"{s.CompanyName}: " +
                    $"{s.Products.Count} products");
            Console.WriteLine(
                $"Commands so far: {CommandLog.Count}");

            db.ChangeTracker.Clear();
            var suppliers = await db.Suppliers
                .OrderBy(s => s.CompanyName)
                .ToListAsync();
            var active = suppliers.Single(s => s.IsActive);
            await db.Entry(active)
                .Collection(s => s.Products).LoadAsync();
            foreach (var s in suppliers)
            {
                var loaded = db.Entry(s)
                    .Collection(x => x.Products).IsLoaded;
                Console.WriteLine(
                    $"{s.CompanyName} loaded: {loaded}");
            }
            Console.WriteLine(
                $"Commands so far: {CommandLog.Count}");
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var active = await db.Suppliers
                .SingleAsync(s => s.IsActive);
            db.Suppliers.Remove(active);
            try
            {
                await db.SaveChangesAsync();
                Console.WriteLine("Deleted the supplier.");
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException
                    { Number: 547 })
            {
                Console.WriteLine("Delete refused: products " +
                    $"still use {active.CompanyName}");
            }
        }
    }

    private static async Task SeedAsync(InventoryDbContext db)
    {
        await Seed.CatalogAsync(db);
        var paper = new Supplier
        {
            CompanyName = "Paper Co.",
            ContactEmail = "orders@paper.example"
        };
        var products = await db.Products
            .Where(p => p.Sku == "BOOK-001" || p.Sku == "PAD-002")
            .ToListAsync();
        foreach (var p in products)
            p.Supplier = paper;
        db.Suppliers.Add(new Supplier
        {
            CompanyName = "Old Pens Ltd.",
            ContactEmail = "sales@oldpens.example",
            IsActive = false
        });
        await db.SaveChangesAsync();
    }
}
