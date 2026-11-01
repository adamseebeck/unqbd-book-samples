using Microsoft.EntityFrameworkCore;

public static class InventoryQueries
{
    public static readonly Func<InventoryDbContext, int,
        IAsyncEnumerable<ProductSummary>> LowStock =
        EF.CompileAsyncQuery(
            (InventoryDbContext db, int threshold) => db.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity < threshold)
                .OrderBy(p => p.Id)
                .Select(p => new ProductSummary(
                    p.Id, p.Name, p.StockQuantity)));
}
