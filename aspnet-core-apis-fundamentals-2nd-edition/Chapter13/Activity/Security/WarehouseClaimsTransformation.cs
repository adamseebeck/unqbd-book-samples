using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace WarehouseApi.Security;

// Adds the claims this API checks when an identity provider sends the same facts under other names
public class WarehouseClaimsTransformation : IClaimsTransformation
{
    private static readonly string[] Roles = [UserRoles.WarehouseStaff, UserRoles.Supervisor, UserRoles.IntegrationClient];
    private const string GroupsClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups";

    // Group IDs from Authorization:GroupRoles and the role each one grants. A group's name proves
    // nothing, because anyone who may create or rename groups in the directory chooses it
    private readonly Dictionary<string, string> _groupRoles;

    public WarehouseClaimsTransformation(IConfiguration configuration)
    {
        _groupRoles = (configuration.GetSection("Authorization:GroupRoles").Get<Dictionary<string, string>>() ?? [])
            .Where(mapping => Roles.Contains(mapping.Value))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
        {
            return Task.FromResult(principal);
        }

        // The JWT handler already maps role and roles claims to ClaimTypes.Role; some providers send
        // group IDs instead, and only the configured ones become roles. At the time of writing the handler
        // keeps the short name groups; a later version may map it to the long name, so read both
        foreach (var claim in principal.FindAll(c => c.Type is "groups" or GroupsClaimType).ToList())
        {
            if (_groupRoles.TryGetValue(claim.Value, out var role) && !identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        // TransformAsync can run more than once for a request, so add each claim only once
        var warehouseClaim = principal.FindFirst("warehouse_id");
        if (warehouseClaim is not null && !identity.HasClaim(c => c.Type == WarehouseClaimTypes.WarehouseId))
        {
            identity.AddClaim(new Claim(WarehouseClaimTypes.WarehouseId, warehouseClaim.Value));
        }

        return Task.FromResult(principal);
    }
}
