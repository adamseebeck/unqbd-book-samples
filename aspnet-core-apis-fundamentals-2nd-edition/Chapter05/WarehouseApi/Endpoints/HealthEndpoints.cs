using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (
            IHealthService healthService,
            CancellationToken cancellationToken) =>
        {
            var health = await healthService.GetHealthAsync(cancellationToken);
            return Results.Ok(health);
        });

        return app;
    }
}
