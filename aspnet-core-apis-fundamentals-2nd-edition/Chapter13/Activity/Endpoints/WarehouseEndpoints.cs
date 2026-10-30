using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        // Any authenticated caller can list the warehouses
        app.MapGet("/api/warehouses", async (
            IWarehouseService service,
            CancellationToken cancellationToken) =>
        {
            var warehouses = await service.GetAllAsync(cancellationToken);
            return Results.Ok(warehouses);
        })
        .RequireAuthorization();

        app.MapGet("/api/warehouses/{id:int}", async (
            int id,
            IWarehouseService service,
            CancellationToken cancellationToken) =>
        {
            var warehouse = await service.GetByIdAsync(id, cancellationToken);
            return warehouse is null
                ? Results.Problem($"Warehouse {id} not found.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(warehouse);
        })
        .RequireAuthorization();

        return app;
    }
}
