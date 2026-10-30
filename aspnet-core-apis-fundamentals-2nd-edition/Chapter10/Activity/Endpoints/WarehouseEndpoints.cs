using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/warehouses", async (
            IWarehouseService service,
            CancellationToken cancellationToken) =>
        {
            var warehouses = await service.GetAllAsync(cancellationToken);
            return Results.Ok(warehouses);
        });

        app.MapGet("/api/warehouses/{id:int}", async (
            int id,
            IWarehouseService service,
            CancellationToken cancellationToken) =>
        {
            var warehouse = await service.GetByIdAsync(id, cancellationToken);
            return warehouse is null ? Results.NotFound() : Results.Ok(warehouse);
        });

        app.MapGet("/api/warehouses/{warehouseCode}/low-stock", async (
            string warehouseCode,
            [AsParameters] PaginationParameters pagination,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            if (!pagination.IsValid)
            {
                return Results.BadRequest(new { Error = "PageNumber must be 1 to 100000 and PageSize 1 to 100" });
            }

            var result = await service.GetLowStockItemsAsync(warehouseCode, pagination, cancellationToken);
            return Results.Ok(result);
        });

        return app;
    }
}
