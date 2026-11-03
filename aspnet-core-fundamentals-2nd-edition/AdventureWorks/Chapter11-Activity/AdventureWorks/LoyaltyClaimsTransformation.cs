using System.Security.Claims;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks;

// Adds the stored loyalty number as a claim. It can run more than
// once per request, so it skips a principal that already has one.
public sealed class LoyaltyClaimsTransformation(
    UserManager<ApplicationUser> users) : IClaimsTransformation
{
    public const string ClaimType = "loyalty_number";

    public async Task<ClaimsPrincipal> TransformAsync(
        ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            principal.HasClaim(c => c.Type == ClaimType))
            return principal;
        var id =
            principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null) return principal;
        var user = await users.FindByIdAsync(id);
        if (user is null || user.LoyaltyNumber.Length == 0)
            return principal;
        var clone = principal.Clone();
        clone.AddIdentity(new ClaimsIdentity(
            [new Claim(ClaimType, user.LoyaltyNumber)]));
        return clone;
    }
}
