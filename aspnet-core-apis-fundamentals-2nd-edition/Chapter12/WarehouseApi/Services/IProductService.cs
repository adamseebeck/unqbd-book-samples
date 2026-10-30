using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IProductService
{
    Task<ProductResponse?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductResponse>> SearchAsync(string? searchTerm, bool? isActive, PaginationParameters pagination, CancellationToken cancellationToken = default);
    Task<ProductResponse?> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> UpdateAsync(string sku, UpdateProductRequest request, CancellationToken cancellationToken = default);
}
