using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class StockAdjustmentEndpoints
{
    public static IEndpointRouteBuilder MapStockAdjustmentEndpoints(this IEndpointRouteBuilder app)
    {
        // Integration clients send the warehouse and SKU in the body rather than the route
        app.MapPost("/api/stock-adjustments", async (
            CreateStockAdjustmentRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IInventoryService service,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return Results.BadRequest(new { Error = "Idempotency-Key header is required" });
            }

            var result = await service.AdjustStockAsync(
                request.WarehouseCode,
                request.Sku,
                request.QuantityChange!.Value,
                request.ReasonCode,
                idempotencyKey,
                ct);

            // The adjustment is applied before the response, so 200 describes it; 202 would not
            if (result.Success)
            {
                return Results.Json(result.StockItem, statusCode: result.StatusCode);
            }

            // This URL names no stock item: one that the body names and that does not exist is 422, not 404
            var status = result.StatusCode == StatusCodes.Status404NotFound
                ? StatusCodes.Status422UnprocessableEntity
                : result.StatusCode;
            return Results.Json(new { Error = result.ErrorMessage }, statusCode: status);
        });

        return app;
    }
}
