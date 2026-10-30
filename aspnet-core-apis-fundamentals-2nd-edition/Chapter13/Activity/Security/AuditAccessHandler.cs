using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace WarehouseApi.Security;

public class AuditAccessHandler : AuthorizationHandler<AuditAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuditAccessRequirement requirement)
    {
        if (context.User.IsInRole(UserRoles.Supervisor))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // An auditor needs a clearance claim at or above the level the policy asks for
        var clearanceClaim = context.User.FindFirst(WarehouseClaimTypes.AuditClearance)?.Value;

        if (context.User.IsInRole(UserRoles.InventoryAuditor) &&
            int.TryParse(clearanceClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var clearance) &&
            clearance >= requirement.MinimumClearanceLevel)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
