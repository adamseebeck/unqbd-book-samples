using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WarehouseApi.Security;

namespace WarehouseApi.Configuration;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddWarehouseAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.RequireSupervisor, policy =>
                policy.RequireRole(UserRoles.Supervisor));

            options.AddPolicy(AuthorizationPolicies.RequireWarehouseAccess, policy =>
                policy.Requirements.Add(new WarehouseAccessRequirement(Permissions.Read)));

            options.AddPolicy(AuthorizationPolicies.CanAdjustStock, policy =>
            {
                policy.Requirements.Add(new WarehouseAccessRequirement(Permissions.Adjust));
            });

            options.AddPolicy(AuthorizationPolicies.RequireIntegrationClientOrSupervisor, policy =>
                policy.RequireRole(UserRoles.IntegrationClient, UserRoles.Supervisor));

            options.AddPolicy(AuthorizationPolicies.CanCreateOrder, policy =>
            {
                policy.RequireRole(UserRoles.IntegrationClient, UserRoles.Supervisor);
                // Client ownership and replay protection need a nonempty subject.
                policy.RequireAssertion(context =>
                    context.User.IsInRole(UserRoles.Supervisor)
                    || !string.IsNullOrEmpty(context.User.FindFirstValue(ClaimTypes.NameIdentifier)));
            });
        });

        // The handler keeps no state and depends on no Scoped service, so one instance serves every request
        services.AddSingleton<IAuthorizationHandler, WarehouseAccessHandler>();

        return services;
    }
}
