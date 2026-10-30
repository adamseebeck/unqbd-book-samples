using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Models;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class ProductService : IProductService
{
    private readonly InMemoryWarehouseStore _store;

    public ProductService(InMemoryWarehouseStore store)
    {
        _store = store;
    }

    public Task<ProductResponse?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult<ProductResponse?>(
                _store.Products.TryGetValue(sku, out var product) ? product.ToResponse() : null);
        }
    }

    public Task<PagedResult<ProductResponse>> SearchAsync(string? searchTerm, bool? isActive, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var query = _store.Products.Values.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            // Sort before paging so a page holds the same products on every request
            var matches = query.OrderBy(p => p.Sku).ToList();
            var items = matches
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .Select(p => p.ToResponse())
                .ToList();

            return Task.FromResult(new PagedResult<ProductResponse>(
                items, matches.Count, pagination.PageNumber, pagination.PageSize));
        }
    }

    public Task<ProductResponse?> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            if (_store.Products.ContainsKey(request.Sku))
            {
                return Task.FromResult<ProductResponse?>(null); // Duplicate SKU
            }

            var product = new Product
            {
                Sku = request.Sku,
                Name = request.Name,
                Description = request.Description,
                UnitPrice = request.UnitPrice,
                IsActive = true
            };

            _store.Products.Add(product.Sku, product);
            return Task.FromResult<ProductResponse?>(product.ToResponse());
        }
    }

    public Task<ProductResponse?> UpdateAsync(string sku, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.Products.TryGetValue(sku, out var product))
            {
                return Task.FromResult<ProductResponse?>(null);
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.UnitPrice = request.UnitPrice;
            // The endpoint has already answered a PUT without isActive with 400
            product.IsActive = request.IsActive!.Value;

            return Task.FromResult<ProductResponse?>(product.ToResponse());
        }
    }
}
