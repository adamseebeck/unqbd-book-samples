using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        // Every endpoint in the group shares this route prefix and the Stock tag
        var stockGroup = app.MapGroup("/api/warehouses/{warehouseCode}/stock")
            .WithTags("Stock");

        // GET /api/warehouses/{warehouseCode}/stock
        stockGroup.MapGet("/", async (
            [AsParameters] PaginationParameters pagination,
            [AsParameters] StockFilterParameters filters,
            IInventoryService service,
            CancellationToken ct) =>
        {
            if (!pagination.IsValid)
            {
                return Results.BadRequest(new { Error = "PageNumber must be 1 to 100000 and PageSize 1 to 100" });
            }

            if (!filters.HasKnownSortKey)
            {
                return Results.BadRequest(new { Error = $"SortBy must be one of: {string.Join(", ", StockFilterParameters.SortKeys)}" });
            }

            // filters.WarehouseCode binds from the {warehouseCode} route value
            var result = await service.GetStockAsync(pagination, filters, ct);
            return Results.Ok(result);
        });

        // GET /api/warehouses/{warehouseCode}/stock/{productSku}
        stockGroup.MapGet("/{productSku:sku}", async (
            string warehouseCode,
            string productSku,
            IInventoryService service,
            CancellationToken ct) =>
        {
            var item = await service.GetStockItemAsync(warehouseCode, productSku, ct);
            return item is null ? Results.NotFound(new { Error = "Stock item not found" }) : Results.Ok(item);
        });

        // POST /api/warehouses/{warehouseCode}/stock/{productSku}/adjustments
        stockGroup.MapPost("/{productSku:sku}/adjustments", async (
            string warehouseCode,
            string productSku,
            StockAdjustmentRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IInventoryService service,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return Results.BadRequest(new { Error = "Idempotency-Key header is required" });
            }

            // Validation has already rejected a missing quantity change and unknown reason codes
            var result = await service.AdjustStockAsync(
                warehouseCode, productSku, request.QuantityChange!.Value, request.ReasonCode, idempotencyKey, ct);

            // The service sets the status code: 200 for an applied adjustment and for its replays;
            // 400, 404, 409, or 422 for a failure
            return result.Success
                ? Results.Json(result.StockItem, statusCode: result.StatusCode)
                : Results.Json(new { Error = result.ErrorMessage }, statusCode: result.StatusCode);
        });

        // PUT /api/warehouses/{warehouseCode}/stock/{productSku} (idempotent replacement)
        stockGroup.MapPut("/{productSku:sku}", async (
            string warehouseCode,
            string productSku,
            UpdateStockRequest request,
            IInventoryService service,
            CancellationToken ct) =>
        {
            // [Range] rejects negative values; a rule that compares two values stays in the handler
            if (request.OnHand < request.Reserved)
            {
                return Results.BadRequest(new { Error = "OnHand cannot be less than Reserved" });
            }

            var outcome = await service.UpdateStockAsync(
                warehouseCode, productSku, request.OnHand, request.Reserved, request.ReorderThreshold, ct);

            // Only the order workflows change reserved units, so a replacement must repeat the stored value
            return outcome switch
            {
                StockChangeOutcome.Applied => Results.NoContent(),
                StockChangeOutcome.NotFound => Results.NotFound(new { Error = "Stock item not found" }),
                _ => Results.Conflict(new { Error = "Reserved must equal the stored value; only the order workflows change it" })
            };
        });

        // DELETE /api/warehouses/{warehouseCode}/stock/{productSku}
        stockGroup.MapDelete("/{productSku:sku}", async (
            string warehouseCode,
            string productSku,
            IInventoryService service,
            CancellationToken ct) =>
        {
            var outcome = await service.DeleteStockAsync(warehouseCode, productSku, ct);

            return outcome switch
            {
                StockChangeOutcome.Applied => Results.NoContent(),
                StockChangeOutcome.NotFound => Results.NotFound(new { Error = "Stock item not found" }),
                StockChangeOutcome.HasHistory => Results.Conflict(new { Error = "The stock item has a recorded history and cannot be deleted" }),
                _ => Results.Conflict(new { Error = "The stock item has reserved units and cannot be deleted" })
            };
        });

        return app;
    }
}
