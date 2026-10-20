using System.Collections.Concurrent;

public sealed class ProductService : IProductService
{
    private readonly ConcurrentDictionary<int, ProductDto> _products = new();
    private int _nextId = 1;
    public ProductService() => _products[1] = new(1, "AdventureWorks Helmet", 29.99m, "ABC-1234");
    public Task<ProductDto?> GetByIdAsync(int id) =>
        Task.FromResult(_products.TryGetValue(id, out var product) ? product : null);
    public Task<ProductDto[]> SearchAsync(string? query, int page, int pageSize) =>
        Task.FromResult(_products.Values
            .Where(p => string.IsNullOrWhiteSpace(query) || p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArray());
    public Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        var product = new ProductDto(Interlocked.Increment(ref _nextId), request.Name, request.Price, request.Sku);
        _products[product.Id] = product;
        return Task.FromResult(product);
    }
    public Task<bool> UpdateAsync(int id, UpdateProductRequest request)
    {
        while (_products.TryGetValue(id, out var current))
        {
            if (_products.TryUpdate(id, current with { Name = request.Name, Price = request.Price }, current))
                return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
    public Task<bool> DeleteAsync(int id) => Task.FromResult(_products.TryRemove(id, out _));
}
