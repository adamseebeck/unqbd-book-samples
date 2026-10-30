using Microsoft.AspNetCore.Http.HttpResults;
using WarehouseApi.Configuration;
using WarehouseApi.Contracts;
using WarehouseApi.Contracts.V2;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class WarehouseEndpoints
{
    // Called on the versioned /api/v{version} group; each endpoint maps to one version
    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        // Any authenticated caller can read the warehouses
        var warehouses = app.MapGroup("/warehouses")
            .WithTags("Warehouses")
            .RequireAuthorization();

        // V1: the warehouse contract and the numeric ID route from Chapter 5
        warehouses.MapGet("/", async (
            IWarehouseService service,
            CancellationToken cancellationToken) =>
        {
            var all = await service.GetAllAsync(cancellationToken);
            return Results.Ok(all);
        })
        .MapToApiVersion(ApiVersionConstants.V1)
        .Produces<IReadOnlyList<WarehouseResponse>>();

        warehouses.MapGet("/{id:int}", async (
            int id,
            IWarehouseService service,
            CancellationToken cancellationToken) =>
        {
            var warehouse = await service.GetByIdAsync(id, cancellationToken);
            return warehouse is null
                ? Results.Problem($"Warehouse {id} not found.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(warehouse);
        })
        .MapToApiVersion(ApiVersionConstants.V1)
        .Produces<WarehouseResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // V2: a warehouse is identified by its code, like in every other route that names one
        warehouses.MapGet("/", async Task<Ok<List<WarehouseResponseV2>>> (
            IWarehouseService service,
            CancellationToken cancellationToken) =>
        {
            var all = await service.GetAllAsync(cancellationToken);
            return TypedResults.Ok(all.Select(ToV2).ToList());
        })
        .MapToApiVersion(ApiVersionConstants.V2);

        warehouses.MapGet("/{code}", async Task<Results<Ok<WarehouseResponseV2>, ProblemHttpResult>> (
            string code,
            IWarehouseService service,
            CancellationToken cancellationToken) =>
        {
            var warehouse = await service.GetByCodeAsync(code, cancellationToken);
            return warehouse is null
                ? TypedResults.Problem($"Warehouse {code} not found.", statusCode: StatusCodes.Status404NotFound)
                : TypedResults.Ok(ToV2(warehouse));
        })
        .MapToApiVersion(ApiVersionConstants.V2)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static WarehouseResponseV2 ToV2(WarehouseResponse warehouse) =>
        new(warehouse.Code, warehouse.Name, warehouse.Location, warehouse.IsOperational);
}
