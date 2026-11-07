using AdventureWorks.Core;
using Microsoft.AspNetCore.Authorization;

namespace AdventureWorks;

// Activity: a shipment hold guarded by the provider's second
// department policy, and a route that shows its closed set.
public static class ShippingHolds
{
    public static IEndpointRouteBuilder MapShippingHolds(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{id:int}/hold", Hold)
            .RequireAuthorization("Department:Shipping");
        app.MapGet("/api/policies/department/{name}", Exists);
        return app;
    }

    static IResult Hold(int id, IOrderRepository orders) =>
        orders.GetById(id) is null
            ? Results.NotFound()
            : Results.Ok(new { OrderId = id, Held = true });

    static async Task<IResult> Exists(string name,
        IAuthorizationPolicyProvider policies) =>
        Results.Ok(new
        {
            Policy = $"Department:{name}",
            Exists = await policies.GetPolicyAsync(
                $"Department:{name}") is not null
        });
}
