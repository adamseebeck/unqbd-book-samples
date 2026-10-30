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
                return Results.Problem("PageNumber must be 1 to 100000 and PageSize 1 to 100", statusCode: StatusCodes.Status400BadRequest);
            }

            if (!filters.HasKnownSortKey)
            {
                return Results.Problem(
                    $"SortBy must be one of: {string.Join(", ", StockFilterParameters.SortKeys)}", statusCode: StatusCodes.Status400BadRequest);
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
            return item is null
                ? Results.Problem("Stock item not found.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(item);
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
                return Results.Problem("Idempotency-Key header is required.", statusCode: StatusCodes.Status400BadRequest);
            }

            // The API accepts keys up to this length, the length of the idempotency table's key column
            if (idempotencyKey.Length > IdempotencyKeyRules.MaxLength)
            {
                return Results.Problem(
                    $"Idempotency-Key must be at most {IdempotencyKeyRules.MaxLength} characters.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Validation has already rejected a missing quantity change and unknown reason codes
            var result = await service.AdjustStockAsync(
                warehouseCode, productSku, request.QuantityChange!.Value, request.ReasonCode, idempotencyKey, ct);

            if (!result.Success)
            {
                // 400, 404, 409, or 422, written through IProblemDetailsService like every other error
                return Results.Problem(result.ErrorMessage, statusCode: result.StatusCode);
            }

            // 200 with the new stock level, for the first request and for its replays
            return Results.Json(result.StockItem, statusCode: result.StatusCode);
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
                return Results.Problem("OnHand cannot be less than Reserved.", statusCode: StatusCodes.Status400BadRequest);
            }

            var outcome = await service.UpdateStockAsync(
                warehouseCode, productSku, request.OnHand, request.Reserved, request.ReorderThreshold, ct);

            // Only the order workflows change reserved units, so a replacement must repeat the stored value
            return outcome switch
            {
                StockChangeOutcome.Applied => Results.NoContent(),
                StockChangeOutcome.NotFound => Results.Problem("Stock item not found.", statusCode: StatusCodes.Status404NotFound),
                _ => Results.Problem(
                    "Reserved must equal the stored value; only the order workflows change it.",
                    statusCode: StatusCodes.Status409Conflict)
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
                StockChangeOutcome.NotFound => Results.Problem("Stock item not found.", statusCode: StatusCodes.Status404NotFound),
                StockChangeOutcome.HasHistory => Results.Problem(
                    "The stock item has a recorded history and cannot be deleted.",
                    statusCode: StatusCodes.Status409Conflict),
                _ => Results.Problem(
                    "The stock item has reserved units and cannot be deleted.",
                    statusCode: StatusCodes.Status409Conflict)
            };
        });

        return app;
    }
}
