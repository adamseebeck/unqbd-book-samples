using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AdventureWorks.Authorization;

public sealed record DepartmentRequirement(string Name)
    : IAuthorizationRequirement;

public sealed class DepartmentHandler
    : AuthorizationHandler<DepartmentRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentRequirement requirement)
    {
        if (context.User.HasClaim("Department", requirement.Name))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

// Builds the Department:Finance and Department:Shipping policies
// on demand. Any other department is unknown: no policy at all.
public sealed class DepartmentPolicyProvider(
    IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    const string Prefix = "Department:";
    static readonly string[] Known = ["Finance", "Shipping"];
    readonly DefaultAuthorizationPolicyProvider fallback =
        new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string name)
    {
        if (!name.StartsWith(Prefix, StringComparison.Ordinal))
            return fallback.GetPolicyAsync(name);
        var department = name[Prefix.Length..];
        if (!Known.Contains(department))
            return Task.FromResult<AuthorizationPolicy?>(null);
        var requirement = new DepartmentRequirement(department);
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
