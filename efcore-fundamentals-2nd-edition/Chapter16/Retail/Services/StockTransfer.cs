using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class StockTransfer
{
    public static async Task RunAsync(IServiceProvider services,
        int sourceId, int targetId, int quantity)
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
            target.StockQuantity += quantity;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }
}
