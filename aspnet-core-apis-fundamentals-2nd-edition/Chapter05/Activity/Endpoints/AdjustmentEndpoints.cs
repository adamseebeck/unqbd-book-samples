using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class AdjustmentEndpoints
{
    public static IEndpointRouteBuilder MapAdjustmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/inventory/adjustments", async (
            StockAdjustmentRequest request,
            IInventoryService inventoryService,
            CancellationToken cancellationToken) =>
        {
            // At most 10,000 units per adjustment, so the new quantity cannot overflow an int
            if (string.IsNullOrWhiteSpace(request.Sku)
                || string.IsNullOrWhiteSpace(request.WarehouseCode)
                || request.Quantity is 0 or < -10_000 or > 10_000)
            {
                return Results.BadRequest(new { Error = "A SKU, a warehouse code, and a quantity from -10000 to 10000, not 0, are required" });
            }

            var result = await inventoryService.AdjustStockAsync(request, cancellationToken);

            // The URL names the adjustments endpoint, which exists; a stock item named in the body
            // that does not exist makes the request unprocessable (422), not the URL wrong (404)
            return result.Status switch
            {
                StockAdjustmentStatus.Applied => Results.Ok(result.StockLevel),
                StockAdjustmentStatus.StockItemNotFound => Results.UnprocessableEntity(
                    new { Error = $"Warehouse {request.WarehouseCode} holds no stock of {request.Sku}" }),
                _ => Results.Conflict(new { Error = "Adjustment would leave fewer units on hand than are reserved" })
            };
        });

        return app;
    }
}
