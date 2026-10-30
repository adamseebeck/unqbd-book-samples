using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Security;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class StockQueryEndpoints
{
    // Called on the versioned /api/v{version} group
    public static IEndpointRouteBuilder MapStockQueryEndpoints(this IEndpointRouteBuilder app)
    {
        // Stock across warehouses, for dashboards and reports: not for one warehouse's staff
        app.MapGet("/stock", async (
            [AsParameters] StockQueryFilter filter,
            ClaimsPrincipal user,
            IAuthorizationService authorizationService,
            IStockQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            // Supervisors read every warehouse. A caller bound to one warehouse reads that warehouse only,
            // the rule that the warehouse stock routes enforce, checked by the same handler
            var warehouseCode = filter.WarehouseCode
                ?? (user.IsInRole(UserRoles.Supervisor) ? null : user.FindFirstValue(WarehouseClaimTypes.WarehouseId));

            var access = await authorizationService.AuthorizeAsync(
                user, warehouseCode, new WarehouseAccessRequirement(Permissions.Read));

            if (!access.Succeeded)
            {
                return Results.Forbid();
            }

            var result = await queryService.QueryAsync(filter with { WarehouseCode = warehouseCode }, cancellationToken);
            return Results.Ok(result);
        })
        .WithTags("Stock")
        .Produces<PagedResult<StockListItem>>()
        .RequireAuthorization(AuthorizationPolicies.RequireIntegrationClientOrSupervisor);

        return app;
    }
}
