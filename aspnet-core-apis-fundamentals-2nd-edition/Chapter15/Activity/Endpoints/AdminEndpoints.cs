using WarehouseApi.Configuration;
using WarehouseApi.Contracts;
using WarehouseApi.Fulfillment;
using WarehouseApi.Security;

namespace WarehouseApi.Endpoints;

public static class AdminEndpoints
{
    // Called on the versioned /api/v{version} group; a new feature, so only in 2.0
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/dead-letter/status", async (
            IFulfillmentDeadLetterQueue deadLetters,
            CancellationToken cancellationToken) =>
        {
            var status = await deadLetters.GetStatusAsync(cancellationToken);
            return Results.Ok(status);
        })
        .MapToApiVersion(ApiVersionConstants.V2)
        .WithTags("Administration")
        .Produces<DeadLetterStatusResponse>()
        .RequireAuthorization(AuthorizationPolicies.RequireSupervisor);

        return app;
    }
}
