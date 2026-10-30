using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.RoutingConstraints;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/warehouses/{code:warehouse}/stock-adjustments", async (
            string code,
            CreateStockAdjustmentRequest request,
            IStockService service,
            CancellationToken cancellationToken) =>
        {
            // The converter also reads numbers, so 99 would bind as an undefined reason
            if (!SkuRouteConstraint.IsValidSku(request.ProductSku)
                || request.QuantityChange == 0
                || request.ReasonCode is not { } reason
                || !Enum.IsDefined(reason))
            {
                return Results.BadRequest(new { Error = "A valid ProductSku, a nonzero QuantityChange, and a known ReasonCode are required" });
            }

            var adjustment = await service.CreateAdjustmentAsync(code, request, cancellationToken);
            return Results.Created($"/api/warehouses/{adjustment.WarehouseCode}/stock-adjustments/{adjustment.Id}", adjustment);
        });

        app.MapGet("/api/warehouses/{code:warehouse}/stock-adjustments/{id:int}", async (
            string code,
            int id,
            IStockService service,
            CancellationToken cancellationToken) =>
        {
            var adjustment = await service.GetAdjustmentAsync(code, id, cancellationToken);
            return adjustment is null ? Results.NotFound() : Results.Ok(adjustment);
        });

        app.MapGet("/api/warehouses/{code:warehouse}/stock-adjustments", async (
            string code,
            [FromQuery] StockAdjustmentReason? reasonCode,
            [FromQuery] DateTimeOffset? dateFrom,
            IStockService service,
            CancellationToken cancellationToken) =>
        {
            var adjustments = await service.GetAdjustmentsAsync(code, reasonCode, dateFrom, cancellationToken);
            return Results.Ok(adjustments);
        });

        return app;
    }
}
