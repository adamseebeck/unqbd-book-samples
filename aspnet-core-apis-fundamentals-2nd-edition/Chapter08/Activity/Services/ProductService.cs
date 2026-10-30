using System.Collections.Concurrent;
using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public class ProductService : IProductService
{
    // The catalog starts with the two products from Chapter 4
    private readonly ConcurrentDictionary<string, ProductResponse> _products = new()
    {
        ["SKU-001"] = new ProductResponse("SKU-001", "Industrial Shelf Bracket", "Steel bracket for pallet racking", 12.99m, true),
        ["SKU-002"] = new ProductResponse("SKU-002", "Heavy Duty Casters (Set of 4)", "Swivel casters for warehouse carts", 45.50m, true)
    };

    public Task<ProductResponse?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        _products.TryGetValue(sku, out var product);
        return Task.FromResult(product);
    }

    public Task<PagedResult<ProductResponse>> SearchAsync(string? searchTerm, bool? isActive, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var query = _products.Values.AsEnumerable();

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
            .ToList();

        return Task.FromResult(new PagedResult<ProductResponse>(
            items, matches.Count, pagination.PageNumber, pagination.PageSize));
    }

    public Task<ProductResponse?> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new ProductResponse(
            request.Sku,
            request.Name,
            request.Description,
            request.UnitPrice,
            true);

        if (!_products.TryAdd(request.Sku, product))
        {
            return Task.FromResult<ProductResponse?>(null); // Duplicate SKU
        }

        return Task.FromResult<ProductResponse?>(product);
    }

    public Task<ProductResponse?> UpdateAsync(string sku, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!_products.ContainsKey(sku))
        {
            return Task.FromResult<ProductResponse?>(null);
        }

        // The endpoint has already answered a PUT without isActive with 400
        var updated = new ProductResponse(
            sku,
            request.Name,
            request.Description,
            request.UnitPrice,
            request.IsActive!.Value);

        _products[sku] = updated;
        return Task.FromResult<ProductResponse?>(updated);
    }
}
