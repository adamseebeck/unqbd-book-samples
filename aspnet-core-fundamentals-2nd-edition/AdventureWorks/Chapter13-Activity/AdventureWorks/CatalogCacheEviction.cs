using AdventureWorks.Core;
using Microsoft.AspNetCore.OutputCaching;

namespace AdventureWorks;

// Chapter 11's decorator idea again: every product write that
// succeeds evicts the cached catalog pages, whichever API, page
// or controller made it. Reads pass straight through.
public sealed class CatalogCacheEviction(IProductService inner,
    IOutputCacheStore cache) : IProductService
{
    public Task<Product?> GetByIdAsync(int id) =>
        inner.GetByIdAsync(id);

    public Task<Product[]> GetByIdsAsync(int[] ids,
        CancellationToken cancellationToken = default) =>
        inner.GetByIdsAsync(ids, cancellationToken);

    public Task<Product?> GetBySkuAsync(string sku,
        CancellationToken cancellationToken = default) =>
        inner.GetBySkuAsync(sku, cancellationToken);

    public Task<Product[]> SearchAsync(
        string? query, int page, int pageSize) =>
        inner.SearchAsync(query, page, pageSize);

    public Task<Product[]> ListByCategoryAsync(string category) =>
        inner.ListByCategoryAsync(category);

    public async Task<Product> CreateAsync(
        CreateProductRequest request)
    {
        var product = await inner.CreateAsync(request);
        await EvictAsync();
        return product;
    }

    public async Task<bool> UpdateAsync(
        int id, UpdateProductRequest request) =>
        await inner.UpdateAsync(id, request) &&
        await EvictAsync();

    public async Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion)
    {
        var outcome = await inner.UpdateAsync(
            id, request, expectedVersion);
        if (outcome == UpdateOutcome.Updated) await EvictAsync();
        return outcome;
    }

    public async Task<bool> DeleteAsync(int id) =>
        await inner.DeleteAsync(id) && await EvictAsync();

    async Task<bool> EvictAsync()
    {
        await cache.EvictByTagAsync(
            StorePerformance.CatalogTag, default);
        return true;
    }
}
