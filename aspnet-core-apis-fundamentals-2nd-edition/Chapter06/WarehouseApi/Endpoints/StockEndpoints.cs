using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class StockEndpoints
{
    private static readonly string[] ValidReasonCodes = ["RECEIPT", "DAMAGE", "CORRECTION", "CYCLE_COUNT", "RETURN"];

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

            // A code outside the list is wrong on its own, so 400, as Chapter 8's attribute answers too
            if (!ValidReasonCodes.Contains(request.ReasonCode))
            {
                return Results.BadRequest(new
                {
                    Error = $"Invalid reason code. Must be one of: {string.Join(", ", ValidReasonCodes)}"
                });
            }

            var result = await service.AdjustStockAsync(
                warehouseCode, productSku, request.QuantityChange, request.ReasonCode, idempotencyKey, ct);

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
            if (request.OnHand < 0 || request.Reserved < 0 || request.ReorderThreshold < 0)
            {
                return Results.BadRequest(new { Error = "OnHand, Reserved, and ReorderThreshold cannot be negative" });
            }

            // The rule that adjustments follow: no fewer units on hand than are reserved
            if (request.OnHand < request.Reserved)
            {
                return Results.BadRequest(new { Error = "OnHand cannot be less than Reserved" });
            }

            var updated = await service.UpdateStockAsync(
                warehouseCode, productSku, request.OnHand, request.Reserved, request.ReorderThreshold, ct);

            return updated ? Results.NoContent() : Results.NotFound(new { Error = "Stock item not found" });
        });

        // DELETE /api/warehouses/{warehouseCode}/stock/{productSku}
        stockGroup.MapDelete("/{productSku:sku}", async (
            string warehouseCode,
            string productSku,
            IInventoryService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteStockAsync(warehouseCode, productSku, ct);

            return deleted ? Results.NoContent() : Results.NotFound(new { Error = "Stock item not found" });
        });

        return app;
    }
}
