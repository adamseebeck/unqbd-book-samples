using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
namespace AdventureWorks.AuthorizationSample;

public sealed record Document(int Id, string Title, string OwnerId, bool Archived = false);
public sealed class DocumentRepository
{
    private readonly ConcurrentDictionary<int, Document> documents = new(new[]
    { new KeyValuePair<int, Document>(1, new Document(1, "Shipping plan", "owner-1")) });
    public Document? Find(int id) => documents.GetValueOrDefault(id);
    public bool Delete(int id) => documents.TryRemove(id, out _);
    public bool Archive(int id)
    {
        while (documents.TryGetValue(id, out var current))
            if (documents.TryUpdate(id, current with { Archived = true }, current)) return true;
        return false;
    }
}
public sealed record MinimumAgeRequirement(int MinimumAge) : IAuthorizationRequirement;
public sealed class MinimumAgeHandler(TimeProvider clock) : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var value = context.User.FindFirstValue("DateOfBirth");
        if (context.User.Identity?.IsAuthenticated == true && DateOnly.TryParseExact(value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var birth))
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            if (birth <= today && birth.AddYears(requirement.MinimumAge) <= today) context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
public sealed class DocumentOwnerRequirement : IAuthorizationRequirement { }
public sealed class DocumentOwnerHandler : AuthorizationHandler<DocumentOwnerRequirement, Document>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        DocumentOwnerRequirement requirement, Document resource)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (context.User.Identity?.IsAuthenticated == true && id is not null && id == resource.OwnerId)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
public sealed record DepartmentRequirement(string Name) : IAuthorizationRequirement;
public sealed class DepartmentHandler : AuthorizationHandler<DepartmentRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DepartmentRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && context.User.HasClaim("Department", requirement.Name))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
public sealed class DynamicPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider fallback = new(options);
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => fallback.GetFallbackPolicyAsync();
    public Task<AuthorizationPolicy?> GetPolicyAsync(string name)
    {
        const string prefix = "Department:";
        if (!name.StartsWith(prefix, StringComparison.Ordinal)) return fallback.GetPolicyAsync(name);
        var department = name[prefix.Length..];
        if (department is not ("Finance" or "Shipping")) return Task.FromResult<AuthorizationPolicy?>(null);
        return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().AddRequirements(new DepartmentRequirement(department)).Build());
    }
}
