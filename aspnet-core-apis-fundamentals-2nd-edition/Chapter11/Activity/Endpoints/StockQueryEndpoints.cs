using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class StockQueryEndpoints
{
    public static IEndpointRouteBuilder MapStockQueryEndpoints(this IEndpointRouteBuilder app)
    {
        // Stock across every warehouse, for dashboards and reports
        app.MapGet("/api/stock", async (
            [AsParameters] StockQueryFilter filter,
            IStockQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var result = await queryService.QueryAsync(filter, cancellationToken);
            return Results.Ok(result);
        });

        return app;
    }
}
