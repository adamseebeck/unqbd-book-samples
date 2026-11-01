using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class Chapter07Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();
        await RetailDatabase.ResetAsync(db, SeedAsync);

        var eager = await db.Products
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .SingleAsync(p => p.Sku == "BOOK-001");
        foreach (var pc in eager.ProductCategories)
            Console.WriteLine($"Eager: {pc.Category.Name}, " +
                $"threshold {pc.StockThreshold}");
        Console.WriteLine($"Commands so far: {CommandLog.Count}");

        db.ChangeTracker.Clear();
        var product = await db.Products
            .SingleAsync(p => p.Sku == "BOOK-001");
        var placements = db.Entry(product)
            .Collection(p => p.ProductCategories);
        Console.WriteLine($"Loaded: {placements.IsLoaded}");
        await placements.LoadAsync();
        var rows = product.ProductCategories.Count;
        Console.WriteLine(
            $"Loaded: {placements.IsLoaded}, rows {rows}");
        Console.WriteLine($"Commands so far: {CommandLog.Count}");

        var customer = await db.Customers
            .Include(c => c.Profile)
            .SingleAsync();
        var name = $"{customer.FirstName} {customer.LastName}";
        var tier = customer.Profile?.LoyaltyTier;
        Console.WriteLine($"Profile: {name}, {tier}");
    }

    private static async Task SeedAsync(InventoryDbContext db)
    {
        await Seed.CatalogAsync(db);
        var notebook = await db.Products
            .SingleAsync(p => p.Sku == "BOOK-001");
        notebook.ProductCategories.Add(new ProductCategory
        {
            Category = new Category { Name = "Gifts" },
            StockThreshold = 3
        });
        var customer = await db.Customers.SingleAsync();
        customer.Profile = new CustomerProfile
        {
            LoyaltyTier = "Gold"
        };
        await db.SaveChangesAsync();
    }
}
