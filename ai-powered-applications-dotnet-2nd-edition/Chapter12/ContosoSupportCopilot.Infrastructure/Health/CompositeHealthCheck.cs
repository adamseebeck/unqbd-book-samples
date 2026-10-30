using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Health;

// Readiness: can this instance serve requests? One status per dependency, in the result's data. The AI
// provider is shared by every replica, so its outage is Degraded, which keeps the readiness probe passing:
// removing every pod would turn fallback drafts and error responses into refused connections. A database
// or the vector store failing is Unhealthy. The AI status comes from the background monitor, so no request
// to readiness sends a chat request.
public class CompositeHealthCheck : IHealthCheck
{
    private const string ProbeVectorKey = "health:probe_vector";
    private static readonly TimeSpan ProbeVectorTimeout = TimeSpan.FromSeconds(1);

    private readonly AIProviderHealthMonitor _aiMonitor;
    private readonly SupportDbContext _supportDb;
    private readonly CopilotDbContext _copilotDb;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IMemoryCache _cache;
    private readonly int? _dimensions;

    public CompositeHealthCheck(
        AIProviderHealthMonitor aiMonitor,
        SupportDbContext supportDb,
        CopilotDbContext copilotDb,
        IEmbeddingClient embeddingClient,
        IVectorSearchService vectorSearch,
        IMemoryCache cache,
        IOptions<VectorStoreOptions> vectorOptions)
    {
        _aiMonitor = aiMonitor;
        _supportDb = supportDb;
        _copilotDb = copilotDb;
        _embeddingClient = embeddingClient;
        _vectorSearch = vectorSearch;
        _cache = cache;
        _dimensions = vectorOptions.Value.Dimensions;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var dependencies = new Dictionary<string, object>
        {
            // The background monitor's last result: Pending, Healthy, or Unhealthy with the reason.
            ["ai_provider"] = _aiMonitor.Status,
            ["support_database"] = await ProbeAsync(() => _supportDb.Database.CanConnectAsync(cancellationToken)),
            ["copilot_database"] = await ProbeAsync(() => _copilotDb.Database.CanConnectAsync(cancellationToken)),
            ["vector_store"] = await CheckVectorStoreAsync(cancellationToken)
        };

        if (dependencies.Where(d => d.Key != "ai_provider").Any(d => d.Value is string status && status.StartsWith("Unhealthy", StringComparison.Ordinal)))
        {
            return HealthCheckResult.Unhealthy("A database or the vector store is unreachable", data: dependencies);
        }

        return dependencies.Values.All(status => Equals(status, "Healthy"))
            ? HealthCheckResult.Healthy("All dependencies are reachable", dependencies)
            : HealthCheckResult.Degraded("The AI provider is unreachable or not checked yet; requests use their fallbacks", data: dependencies);
    }

    // The probe vector needs the stored length. VectorStore:Dimensions gives it without a model call, and
    // Azure AI Search requires that setting. Only the in-memory store without one embeds a probe text, once,
    // and waits at most a second; when that fails, the embedding model is at fault, so the store reports
    // Unknown, which counts as Degraded, instead of Unhealthy.
    private async Task<string> CheckVectorStoreAsync(CancellationToken cancellationToken)
    {
        float[] vector;
        if (_dimensions is int dimensions)
        {
            vector = new float[dimensions];
            vector[0] = 1;
        }
        else
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(ProbeVectorTimeout);
            try
            {
                vector = (await _cache.GetOrCreateAsync(ProbeVectorKey, async entry =>
                {
                    // A size, so the entry also fits a cache that sets MemoryCacheOptions.SizeLimit (Chapter 6).
                    entry.Size = 1;
                    return (await _embeddingClient.GenerateAsync("health check", cancellationToken: deadline.Token)).Vector;
                }))!;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return "Unknown (the probe vector needs the embedding model, which did not answer)";
            }
        }

        return await ProbeAsync(async () =>
        {
            await _vectorSearch.SearchAsync(vector, new VectorSearchRequest { TopK = 1 }, cancellationToken);
            return true;
        });
    }

    private static async Task<string> ProbeAsync(Func<Task<bool>> probe)
    {
        try
        {
            return await probe() ? "Healthy" : "Unhealthy";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"Unhealthy ({ex.GetType().Name})";
        }
    }
}
