using WarehouseApi.Contracts;
using WarehouseApi.Security;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/stock-adjustments/audit: the stock history of every warehouse, newest first
        app.MapGet("/api/stock-adjustments/audit", async (
            [AsParameters] PaginationParameters pagination,
            IStockAuditService auditService,
            CancellationToken cancellationToken) =>
        {
            if (!pagination.IsValid)
            {
                return Results.Problem("PageNumber must be 1 to 100000 and PageSize 1 to 100", statusCode: StatusCodes.Status400BadRequest);
            }

            var history = await auditService.GetHistoryAsync(pagination, cancellationToken);
            return Results.Ok(history);
        })
        .RequireAuthorization(AuthorizationPolicies.CanViewAuditHistory);

        return app;
    }
}
