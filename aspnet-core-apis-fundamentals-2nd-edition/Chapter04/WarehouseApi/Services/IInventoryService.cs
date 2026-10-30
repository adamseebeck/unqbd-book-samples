using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IInventoryService
{
    Task<ProductSummary?> GetProductAsync(string sku, CancellationToken cancellationToken = default);
    Task<StockLevelResponse?> GetStockLevelAsync(string warehouseCode, string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductSummary>> SearchProductsAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ProductSummary?> CreateProductAsync(ProductSummary product, CancellationToken cancellationToken = default);
    Task<ProductSummary?> UpdateProductAsync(string sku, ProductSummary product, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(string sku, CancellationToken cancellationToken = default);
    Task<StockAdjustmentResult> AdjustStockAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default);
}
