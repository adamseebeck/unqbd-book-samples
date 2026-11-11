using System.Globalization;
using System.Security.Claims;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Authorization;

namespace AdventureWorks.Authorization;

public sealed record MinimumAgeRequirement(int MinimumAge)
    : IAuthorizationRequirement;

public sealed class MinimumAgeHandler(TimeProvider clock)
    : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        var value = context.User.FindFirstValue("DateOfBirth");
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var born))
        {
            var today = DateOnly.FromDateTime(
                clock.GetUtcNow().UtcDateTime);
            if (born <= today && requirement.MinimumAge >= 0 &&
                requirement.MinimumAge <= DateOnly.MaxValue.Year - born.Year &&
                born.AddYears(requirement.MinimumAge) <= today)
                context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

// Resource-based: decided only after the order has been loaded.
public sealed record OrderAccessRequirement(bool AllowFinance)
    : IAuthorizationRequirement;

public sealed class OrderAccessHandler
    : AuthorizationHandler<OrderAccessRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderAccessRequirement requirement, Order order)
    {
        var user = context.User;
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is not null && id == order.OwnerId)
            context.Succeed(requirement);
        else if (requirement.AllowFinance &&
            user.HasClaim("Department", "Finance"))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
