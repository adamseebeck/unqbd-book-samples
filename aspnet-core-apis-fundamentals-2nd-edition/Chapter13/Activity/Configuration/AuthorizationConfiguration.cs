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
            {
                policy.RequireRole(UserRoles.IntegrationClient, UserRoles.Supervisor);
            });

            // Supervisors, or inventory auditors with clearance level 2 or higher
            options.AddPolicy(AuthorizationPolicies.CanViewAuditHistory, policy =>
                policy.Requirements.Add(new AuditAccessRequirement(minimumClearanceLevel: 2)));
        });

        // The handler keeps no state and depends on no Scoped service, so one instance serves every request
        services.AddSingleton<IAuthorizationHandler, WarehouseAccessHandler>();
        services.AddSingleton<IAuthorizationHandler, AuditAccessHandler>();

        return services;
    }
}
