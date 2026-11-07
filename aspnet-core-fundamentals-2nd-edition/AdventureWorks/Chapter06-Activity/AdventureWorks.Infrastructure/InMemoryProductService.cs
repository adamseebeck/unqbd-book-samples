using System.Collections.Concurrent;
using AdventureWorks.Core;

namespace AdventureWorks.Infrastructure;

public sealed class InMemoryProductService : IProductService
{
    private readonly ConcurrentDictionary<int, Product> _products
        = new();
    private int _lastId;

    public InMemoryProductService()
    {
        Add("Trail Helmet", "HLM-1001", 29.99m, "accessories");
        Add("Trail Jersey", "JRS-2001", 49.99m, "clothing");
        Add("Mountain-200 Bike", "BIK-3001", 1299.00m, "bikes");
        Add("Two-Person Tent", "TNT-4001", 189.00m, "camping");
    }

    public Task<Product?> GetByIdAsync(int id) =>
        Task.FromResult(_products.GetValueOrDefault(id));

    public Task<Product[]> SearchAsync(
        string? query, int page, int pageSize) =>
        Task.FromResult(_products.Values
            .Where(p => string.IsNullOrWhiteSpace(query) ||
                p.Name.Contains(query,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToArray());

    public Task<Product[]> ListByCategoryAsync(string category) =>
        Task.FromResult(_products.Values
            .Where(p => p.Category.Equals(category,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Id)
            .ToArray());

    public Task<Product> CreateAsync(
        CreateProductRequest request) =>
        Task.FromResult(Add(request.Name, request.Sku,
            request.Price, request.Category));

    public Task<bool> UpdateAsync(
        int id, UpdateProductRequest request)
    {
        while (_products.TryGetValue(id, out var current))
        {
            var changed = current with
            {
                Name = request.Name,
                Price = request.Price,
                Version = current.Version + 1
            };
            if (_products.TryUpdate(id, changed, current))
                return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion)
    {
        while (_products.TryGetValue(id, out var current))
        {
            if (current.Version != expectedVersion)
                return Task.FromResult(UpdateOutcome.Conflict);
            var changed = current with
            {
                Name = request.Name,
                Price = request.Price,
                Version = current.Version + 1
            };
            if (_products.TryUpdate(id, changed, current))
                return Task.FromResult(UpdateOutcome.Updated);
        }
        return Task.FromResult(UpdateOutcome.NotFound);
    }

    public Task<bool> DeleteAsync(int id) =>
        Task.FromResult(_products.TryRemove(id, out _));

    private Product Add(string name, string sku,
        decimal price, string category)
    {
        var id = Interlocked.Increment(ref _lastId);
        var product = new Product(
            id, name, sku, price, category, Version: 1);
        _products[id] = product;
        return product;
    }
}
