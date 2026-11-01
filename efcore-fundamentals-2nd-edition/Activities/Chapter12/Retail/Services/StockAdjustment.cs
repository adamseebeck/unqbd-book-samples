using Microsoft.EntityFrameworkCore;

public static class StockAdjustment
{
    public static async Task SaveAsync(
        InventoryDbContext db, Product product, int delta)
    {
        if (delta == 0)
            throw new ArgumentOutOfRangeException(nameof(delta));
        Apply(product, delta);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.SaveChangesAsync();
                return;
            }
            catch (DbUpdateConcurrencyException)
                when (attempt < 3)
            {
                var entry = db.Entry(product);
                var current = await entry.GetDatabaseValuesAsync()
                    ?? throw new InvalidOperationException(
                        "The product was deleted.");
                entry.OriginalValues.SetValues(current);
                entry.CurrentValues.SetValues(current);
                Apply(product, delta);
                var stock = product.StockQuantity;
                Console.WriteLine(
                    $"Retry {attempt}: stock {stock}");
            }
        }
    }

    private static void Apply(Product product, int delta)
    {
        var stock = checked(product.StockQuantity + delta);
        if (stock < 0)
            throw new InvalidOperationException(
                "Insufficient stock.");
        product.StockQuantity = stock;
    }
}
