namespace AdventureWorks.Core;

public sealed record Product(int Id, string Name, string Sku,
    decimal Price, string Category, int Version);

public static class ProductCategories
{
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bikes", "clothing", "accessories", "camping"
        };
}

public enum UpdateOutcome { Updated, NotFound, Conflict }

public interface IProductService
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product[]> SearchAsync(
        string? query, int page, int pageSize);
    Task<Product[]> ListByCategoryAsync(string category);
    Task<Product> CreateAsync(CreateProductRequest request);
    Task<bool> UpdateAsync(int id, UpdateProductRequest request);
    Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion);
    Task<bool> DeleteAsync(int id);
}
