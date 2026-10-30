using Microsoft.AspNetCore.Authorization;

namespace WarehouseApi.Security;

public class WarehouseAccessHandler : AuthorizationHandler<WarehouseAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WarehouseAccessRequirement requirement)
    {
        // Supervisors work with every warehouse
        if (context.User.IsInRole(UserRoles.Supervisor))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // An endpoint policy receives the HttpContext; code that loaded a resource passes its warehouse code
        var warehouseCode = context.Resource switch
        {
            HttpContext httpContext => httpContext.GetRouteValue("warehouseCode") as string,
            string code => code,
            _ => null
        };

        if (string.IsNullOrEmpty(warehouseCode))
        {
            return Task.CompletedTask;
        }

        // The caller's warehouse comes from the token; SQL Server compares codes without case, so this does too
        var userWarehouseCode = context.User.FindFirst(WarehouseClaimTypes.WarehouseId)?.Value;
        if (!string.Equals(userWarehouseCode, warehouseCode, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        bool hasPermission = requirement.RequiredPermission switch
        {
            Permissions.Read => context.User.IsInRole(UserRoles.WarehouseStaff) ||
                               context.User.IsInRole(UserRoles.IntegrationClient),
            Permissions.Adjust => context.User.IsInRole(UserRoles.WarehouseStaff),
            Permissions.Fulfill => context.User.IsInRole(UserRoles.WarehouseStaff),
            _ => false
        };

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
