using Microsoft.AspNetCore.Authorization;

namespace AdventureWorks.Authorization;

public static class StorePolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AtLeast21 = "AtLeast21";
    public const string OrderAccess = "OrderAccess";
    public const string OrderOwner = "OrderOwner";
    public const string FinanceDepartment = "Department:Finance";

    public static IServiceCollection AddStorePolicies(
        this IServiceCollection services)
    {
        // Every endpoint needs a signed-in user unless it says
        // otherwise: public endpoints call AllowAnonymous.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build())
            .AddPolicy(AdminOnly, p => p.RequireRole("Admin"))
            .AddPolicy(AtLeast21, p => p.AddRequirements(
                new MinimumAgeRequirement(21)))
            .AddPolicy(OrderAccess, p => p.AddRequirements(
                new OrderAccessRequirement(AllowFinance: true)))
            .AddPolicy(OrderOwner, p => p.AddRequirements(
                new OrderAccessRequirement(AllowFinance: false)));
        services.AddSingleton<IAuthorizationHandler,
            MinimumAgeHandler>();
        services.AddSingleton<IAuthorizationHandler,
            OrderAccessHandler>();
        services.AddScoped<IAuthorizationHandler,
            DepartmentHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider,
            DepartmentPolicyProvider>();
        return services;
    }
}
