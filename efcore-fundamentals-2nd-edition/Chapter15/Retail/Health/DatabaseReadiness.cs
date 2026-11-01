using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class DatabaseReadiness(IServiceScopeFactory scopes)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider
            .GetRequiredService<InventoryDbContext>();
        var canConnect = await db.Database
            .CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database unavailable");
    }
}
