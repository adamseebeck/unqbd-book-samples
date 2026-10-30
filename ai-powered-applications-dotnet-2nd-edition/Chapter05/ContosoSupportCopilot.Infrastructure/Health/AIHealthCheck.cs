using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Health;

// Registered as a singleton, so a cached result survives from one health request to the next.
public class AIHealthCheck : IHealthCheck
{
    // A check sends a model request; reusing the result bounds what frequent requests cost.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    // Shorter than the registration's 10-second limit, so a slow provider is cached as Unhealthy too.
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(8);

    private readonly AIConnectivityValidator _validator;
    private readonly AIProvider _provider;
    private CachedResult? _cached;

    public AIHealthCheck(AIConnectivityValidator validator, IOptions<AIProviderOptions> options)
    {
        _validator = validator;
        _provider = options.Value.Provider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var cached = Volatile.Read(ref _cached);
        if (cached is not null && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return cached.Result;
        }

        HealthCheckResult result;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);
        try
        {
            var isHealthy = await _validator.ValidateAsync(timeout.Token);
            result = isHealthy
                ? HealthCheckResult.Healthy($"AI provider {_provider} is ready")
                : HealthCheckResult.Unhealthy($"AI provider {_provider} is unreachable");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            result = HealthCheckResult.Unhealthy($"AI provider {_provider} did not answer within {CheckTimeout.TotalSeconds} seconds");
        }

        Volatile.Write(ref _cached, new CachedResult(result, DateTimeOffset.UtcNow + CacheDuration));
        return result;
    }

    private sealed record CachedResult(HealthCheckResult Result, DateTimeOffset ExpiresAt);
}
