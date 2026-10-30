using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Get stock level for a specific warehouse and SKU
        app.MapGet("/api/warehouses/{warehouseCode}/stock/{sku}", async (
            string warehouseCode,
            string sku,
            IInventoryService inventoryService,
            CancellationToken cancellationToken) =>
        {
            var stock = await inventoryService.GetStockLevelAsync(warehouseCode, sku, cancellationToken);
            return stock is null ? Results.NotFound() : Results.Ok(stock);
        });

        return app;
    }
}
