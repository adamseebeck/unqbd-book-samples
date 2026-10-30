using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class Chapter08Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
            await RetailDatabase.ResetAsync(
                scope.GetContext(), Seed.CatalogAsync);

        using (var first = services.CreateScope())
        using (var second = services.CreateScope())
        {
            var a = await first.GetContext().Products
                .SingleAsync(p => p.Sku == "BOOK-001");
            var b = await second.GetContext().Products
                .SingleAsync(p => p.Sku == "BOOK-001");
            a.Price = 20m;
            a.StockQuantity -= 2;
            await first.GetContext().SaveChangesAsync();
            Console.WriteLine("First edit: price 20, stock 8");
            var secondDb = second.GetContext();
            await StockAdjustment.SaveAsync(secondDb, b, -3);
        }

        int notebookId, padId;
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var notebook = await db.Products
                .SingleAsync(p => p.Sku == "BOOK-001");
            Console.WriteLine(
                $"Saved: price {notebook.Price:F2}, " +
                $"stock {notebook.StockQuantity}");

            var customerId = await db.Customers
                .Select(c => c.Id).SingleAsync();
            var orderId = await OrderService.PlaceAsync(
                db, customerId, notebook.Id, quantity: 2);
            var total = await db.OrderItems
                .Where(i => i.OrderId == orderId)
                .SumAsync(i => i.Quantity * i.UnitPrice);
            Console.WriteLine($"Order total: {total:F2}");

            notebookId = notebook.Id;
            padId = await db.Products
                .Where(p => p.Sku == "PAD-002")
                .Select(p => p.Id).SingleAsync();
        }

        await StockTransfer.RunAsync(
            services, notebookId, padId, quantity: 1);
        using (var scope = services.CreateScope())
        {
            var stock = await scope.GetContext().Products
                .OrderBy(p => p.Id)
                .Select(p => p.Sku + " " + p.StockQuantity)
                .ToListAsync();
            Console.WriteLine("After transfer: " +
                string.Join(", ", stock));
        }
    }
}
