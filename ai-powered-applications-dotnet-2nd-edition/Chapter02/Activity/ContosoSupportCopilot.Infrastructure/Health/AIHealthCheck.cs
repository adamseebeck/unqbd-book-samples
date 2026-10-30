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
    private readonly AIProviderOptions _options;
    private CachedResult? _cached;

    public AIHealthCheck(AIConnectivityValidator validator, IOptions<AIProviderOptions> options)
    {
        _validator = validator;
        _options = options.Value;
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

        ConnectivityResult connectivity;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);
        try
        {
            connectivity = await _validator.ValidateAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            connectivity = new ConnectivityResult(false, _options.Provider.ToString(), _options.ChatDeployment,
                $"No answer within {CheckTimeout.TotalSeconds} seconds", (long)CheckTimeout.TotalMilliseconds);
        }

        var data = new Dictionary<string, object>
        {
            ["provider"] = connectivity.Provider,
            ["chatDeployment"] = connectivity.ChatDeployment ?? "(none)",
            ["elapsedMs"] = connectivity.ElapsedMilliseconds
        };

        var result = connectivity.IsConnected
            ? HealthCheckResult.Healthy(connectivity.Message, data)
            : HealthCheckResult.Unhealthy($"AI provider {connectivity.Provider} is unreachable: {connectivity.Message}", data: data);

        Volatile.Write(ref _cached, new CachedResult(result, DateTimeOffset.UtcNow + CacheDuration));
        return result;
    }

    private sealed record CachedResult(HealthCheckResult Result, DateTimeOffset ExpiresAt);
}
