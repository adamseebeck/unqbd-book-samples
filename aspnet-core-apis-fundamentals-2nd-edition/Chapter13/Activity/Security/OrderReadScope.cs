using System.Security.Claims;

namespace WarehouseApi.Security;

// The orders a caller may read: supervisors every order, warehouse staff the orders their warehouse
// ships, integration clients the orders they created. The order list and a single order use this rule
public record OrderReadScope(bool AllOrders, string? WarehouseCode, string? CreatedBy)
{
    // Every order: what supervisors read, and what code reads after it has authorized the caller in another way
    public static readonly OrderReadScope All = new(true, null, null);

    public static OrderReadScope For(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Supervisor)
            ? All
            : new OrderReadScope(
                false,
                user.IsInRole(UserRoles.WarehouseStaff) ? user.FindFirstValue(WarehouseClaimTypes.WarehouseId) : null,
                user.IsInRole(UserRoles.IntegrationClient) ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null);
}
