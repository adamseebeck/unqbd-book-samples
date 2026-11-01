using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class Chapter03Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            await RetailDatabase.ResetAsync(db);
            await Seed.ProductAsync(db);
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var product = await db.Products
                .Include(p => p.Category)
                .SingleAsync();
            Console.WriteLine($"{product.Sku}: {product.Name}, " +
                $"{product.Category.Name}, {product.Price:F2}");
        }
    }
}
