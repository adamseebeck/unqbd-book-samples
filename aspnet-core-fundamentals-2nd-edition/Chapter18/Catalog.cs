using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed record Product(int Id, string Name);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Product[]))]
public partial class DeploymentJsonContext : JsonSerializerContext { }
public sealed class CatalogStore(IWebHostEnvironment environment, IConfiguration configuration)
{
    public async Task<Product[]> LoadAsync(CancellationToken ct)
    {
        var path = configuration["CatalogPath"] ?? Path.Combine(environment.ContentRootPath, "catalog.json");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync(stream, DeploymentJsonContext.Default.ProductArray, ct)
            ?? throw new InvalidDataException("Catalog is empty.");
    }
}
public sealed class CatalogHealthCheck(CatalogStore catalog) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await catalog.LoadAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return HealthCheckResult.Unhealthy("Catalog is unavailable.");
        }
    }
}
