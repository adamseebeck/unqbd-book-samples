using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class Chapter05Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
            await RetailDatabase.ResetAsync(scope.GetContext());

        int productId;
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var product = new Product
            {
                Sku = "BOOK-001", Name = "C# Notebook",
                Price = 12.50m, StockQuantity = 10,
                Category = new Category { Name = "Books" }
            };
            db.Products.Add(product);
            var entry = db.Entry(product);
            Console.WriteLine($"After Add: {entry.State}");
            await db.SaveChangesAsync();
            Console.WriteLine(
                $"After save: {entry.State}, Id = {product.Id}");

            product.Price = 13.75m;
            db.ChangeTracker.DetectChanges();
            Console.WriteLine($"After edit: {entry.State}");
            await db.SaveChangesAsync();
            productId = product.Id;
        }

        var edit = new PriceEdit(productId, 14.25m);
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var saved = await ApplyPriceEditAsync(db, edit);
            Console.WriteLine($"Edited price: {saved:F2}");
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var product = await db.Products.SingleAsync(
                p => p.Id == productId);
            product.StockQuantity -= 2;
            var order = new Order
            {
                Customer = new Customer
                {
                    FirstName = "Alex", LastName = "Lee",
                    Email = "alex@example.test",
                    CreatedAt = DateTime.UtcNow
                },
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderItems =
                [
                    new OrderItem
                    {
                        ProductId = product.Id, Quantity = 2,
                        UnitPrice = product.Price
                    }
                ]
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            var total = order.OrderItems
                .Sum(i => i.Quantity * i.UnitPrice);
            Console.WriteLine($"Order total: {total:F2}");
            Console.WriteLine($"Stock: {product.StockQuantity}");
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var product = await db.Products.SingleAsync(
                p => p.Id == productId);
            db.Products.Remove(product);
            try
            {
                await db.SaveChangesAsync();
                Console.WriteLine("Deleted the product.");
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException
                    { Number: 547 })
            {
                Console.WriteLine(
                    "Delete refused: an order line uses it.");
            }
        }
    }

    private static async Task<decimal> ApplyPriceEditAsync(
        InventoryDbContext db, PriceEdit edit)
    {
        if (edit.Price is < 0 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(edit));
        var product = await db.Products.SingleAsync(
            p => p.Id == edit.ProductId);
        product.Price = edit.Price;
        await db.SaveChangesAsync();
        return product.Price;
    }
}

public sealed record PriceEdit(int ProductId, decimal Price);
