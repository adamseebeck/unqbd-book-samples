using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class Chapter06Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.GetContext();
        await RetailDatabase.ResetAsync(db, Seed.CatalogAsync);

        var lowStock = db.Products
            .Where(p => p.StockQuantity < 20)
            .OrderBy(p => p.Price).ThenBy(p => p.Id);
        var firstPage = await lowStock
            .Select(p => new { p.Id, p.Name, p.Price })
            .Take(2)
            .ToListAsync();
        foreach (var row in firstPage)
            Console.WriteLine($"Page 1: {row.Id} {row.Name}");
        var tracked = db.ChangeTracker.Entries().Count();
        Console.WriteLine($"Tracked entities: {tracked}");

        var last = firstPage[^1];
        var nextPage = await lowStock
            .Where(p => p.Price > last.Price
                || (p.Price == last.Price && p.Id > last.Id))
            .Select(p => new { p.Id, p.Name, p.Price })
            .Take(2)
            .ToListAsync();
        foreach (var row in nextPage)
            Console.WriteLine($"Page 2: {row.Id} {row.Name}");

        var stockByCategory = await db.Products
            .GroupBy(p => p.Category.Name)
            .Select(g => new
            {
                Category = g.Key,
                Products = g.Count(),
                Stock = g.Sum(p => p.StockQuantity)
            })
            .OrderBy(g => g.Category)
            .ToListAsync();
        foreach (var g in stockByCategory)
            Console.WriteLine($"{g.Category}: " +
                $"products {g.Products}, stock {g.Stock}");

        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .AsSingleQuery()
            .SingleAsync();
        foreach (var item in order.OrderItems)
        {
            var product = item.Product;
            Console.WriteLine($"Order {order.Id}: " +
                $"{item.Quantity} x {product.Name} " +
                $"({product.Category.Name})");
        }
    }
}
