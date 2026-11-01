using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class TransactionCheck
{
    public static async Task RunAsync(IServiceProvider services)
    {
        int notebookId, padId;
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            await RetailDatabase.ResetAsync(
                db, Seed.CatalogAsync);
            notebookId = await db.Products
                .Where(p => p.Sku == "BOOK-001")
                .Select(p => p.Id).SingleAsync();
            padId = await db.Products
                .Where(p => p.Sku == "PAD-002")
                .Select(p => p.Id).SingleAsync();
        }

        await FreshStockAdjustment.AdjustAsync(
            services, notebookId, -3, async () =>
            {
                using var other = services.CreateScope();
                var db = other.GetContext();
                var product = await db.Products
                    .SingleAsync(p => p.Id == notebookId);
                product.StockQuantity -= 2;
                await db.SaveChangesAsync();
            });
        await PrintAsync(services, "After adjustment");

        try
        {
            await TransferAsync(services, notebookId, padId, 2,
                fail: true);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Transfer failed: {ex.Message}");
        }
        await PrintAsync(services, "After failed transfer");

        await TransferAsync(services, notebookId, padId, 2,
            fail: false);
        await PrintAsync(services, "After transfer");
    }

    private static async Task TransferAsync(
        IServiceProvider services, int sourceId, int targetId,
        int quantity, bool fail)
    {
        if (sourceId == targetId || quantity <= 0)
            throw new ArgumentException("Invalid transfer.");
        using var outer = services.CreateScope();
        var strategy = outer.ServiceProvider
            .GetRequiredService<InventoryDbContext>()
            .Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();
            await using var transaction =
                await db.Database.BeginTransactionAsync();
            var source = await db.Products
                .SingleAsync(p => p.Id == sourceId);
            var target = await db.Products
                .SingleAsync(p => p.Id == targetId);
            if (source.StockQuantity < quantity)
                throw new InvalidOperationException(
                    "Insufficient stock.");

            source.StockQuantity -= quantity;
            await db.SaveChangesAsync();
            if (fail)
                throw new InvalidOperationException(
                    "Simulated failure after the first save.");
            target.StockQuantity += quantity;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    private static async Task PrintAsync(
        IServiceProvider services, string label)
    {
        using var scope = services.CreateScope();
        var stock = await scope.GetContext().Products
            .Where(p => p.Sku == "BOOK-001" || p.Sku == "PAD-002")
            .OrderBy(p => p.Id)
            .Select(p => p.Sku + " " + p.StockQuantity)
            .ToListAsync();
        Console.WriteLine(
            $"{label}: " + string.Join(", ", stock));
    }
}
