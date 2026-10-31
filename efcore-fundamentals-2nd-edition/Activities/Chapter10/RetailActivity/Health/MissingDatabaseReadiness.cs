using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class MissingDatabaseReadiness(
    IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnectionStringBuilder(
            configuration.GetConnectionString(
                "DefaultConnection") ?? "")
        {
            InitialCatalog = "EfCoreBook_Missing"
        };
        var options =
            new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlServer(connection.ConnectionString)
                .Options;
        await using var db = new InventoryDbContext(options);
        return await db.Database
            .CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Missing database");
    }
}
