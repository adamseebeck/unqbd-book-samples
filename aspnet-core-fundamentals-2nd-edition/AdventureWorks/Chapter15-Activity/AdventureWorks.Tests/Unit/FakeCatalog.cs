using AdventureWorks.Core;

namespace AdventureWorks.Tests.Unit;

// The catalog OrderService reads prices from. Only the method
// the service calls is real; the rest fail if a test calls one.
public sealed class FakeCatalog(params Product[] products)
    : IProductService
{
    public Task<Product[]> GetByIdsAsync(int[] ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(products
            .Where(p => ids.Contains(p.Id)).ToArray());

    public Task<Product?> GetByIdAsync(int id) =>
        throw new NotSupportedException();

    public Task<Product?> GetBySkuAsync(string sku,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Product[]> SearchAsync(
        string? query, int page, int pageSize) =>
        throw new NotSupportedException();

    public Task<Product[]> ListByCategoryAsync(string category) =>
        throw new NotSupportedException();

    public Task<Product> CreateAsync(
        CreateProductRequest request) =>
        throw new NotSupportedException();

    public Task<bool> UpdateAsync(
        int id, UpdateProductRequest request) =>
        throw new NotSupportedException();

    public Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(int id) =>
        throw new NotSupportedException();
}
