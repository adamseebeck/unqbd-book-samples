using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public sealed record ProductSearchResult(int Id, string Name,
    string CategoryName, int StockQuantity);

public interface IProductSearch
{
    Task<List<ProductSearchResult>> SearchAsync(int? categoryId,
        int? threshold, int page, int pageSize);
}

public sealed class ProductSearchSpecification(
    int? categoryId, int? threshold)
{
    public Expression<Func<Product, bool>> Criteria =>
        p => (categoryId == null || p.CategoryId == categoryId)
            && (threshold == null || p.StockQuantity < threshold);
}

public sealed class ProductSearch(InventoryDbContext db)
    : IProductSearch
{
    public Task<List<ProductSearchResult>> SearchAsync(
        int? categoryId, int? threshold, int page, int pageSize)
    {
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(
                nameof(threshold));
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize is < 1 or > 50)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize));
        var search = new ProductSearchSpecification(
            categoryId, threshold);
        return db.Products
            .AsNoTracking()
            .Where(search.Criteria)
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductSearchResult(p.Id, p.Name,
                p.Category.Name, p.StockQuantity))
            .ToListAsync();
    }
}

public sealed class StubProductSearch : IProductSearch
{
    public Task<List<ProductSearchResult>> SearchAsync(
        int? categoryId, int? threshold, int page,
        int pageSize) =>
        Task.FromResult(new List<ProductSearchResult>
        {
            new(0, "Sample Item", "Samples", 1)
        });
}
