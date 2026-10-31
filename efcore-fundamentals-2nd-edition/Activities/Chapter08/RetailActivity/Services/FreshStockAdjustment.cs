using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class FreshStockAdjustment
{
    public static async Task AdjustAsync(
        IServiceProvider services, int productId, int delta,
        Func<Task>? beforeFirstSave = null)
    {
        if (delta == 0)
            throw new ArgumentOutOfRangeException(nameof(delta));
        for (var attempt = 1; ; attempt++)
        {
            using var scope = services.CreateScope();
            var db = scope.GetContext();
            var product = await db.Products
                .SingleAsync(p => p.Id == productId);
            var stock = checked(product.StockQuantity + delta);
            if (stock < 0)
                throw new InvalidOperationException(
                    "Insufficient stock.");
            product.StockQuantity = stock;
            if (attempt == 1 && beforeFirstSave is not null)
                await beforeFirstSave();
            try
            {
                await db.SaveChangesAsync();
                Console.WriteLine(
                    $"Attempt {attempt}: saved stock {stock}");
                return;
            }
            catch (DbUpdateConcurrencyException)
                when (attempt < 3)
            {
                Console.WriteLine($"Attempt {attempt}: conflict");
            }
        }
    }
}
