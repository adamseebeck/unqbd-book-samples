using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public sealed record ProductSummary(
    int Id, string Name, int StockQuantity);

public interface IInventoryReader
{
    Task<List<ProductSummary>> LowStockAsync(int threshold);
}

public sealed class LowStockSpecification(int threshold)
{
    public Expression<Func<Product, bool>> Criteria =>
        p => p.StockQuantity < threshold;
}

public sealed class InventoryReader(InventoryDbContext db)
    : IInventoryReader
{
    public Task<List<ProductSummary>> LowStockAsync(int threshold)
    {
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(
                nameof(threshold));
        var lowStock = new LowStockSpecification(threshold);
        return db.Products
            .AsNoTracking()
            .Where(lowStock.Criteria)
            .OrderBy(p => p.Id)
            .Select(p => new ProductSummary(
                p.Id, p.Name, p.StockQuantity))
            .ToListAsync();
    }
}
