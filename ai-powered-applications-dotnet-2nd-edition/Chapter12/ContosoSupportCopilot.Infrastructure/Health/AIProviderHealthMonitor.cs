using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Health;

// Probes the AI provider in the background and keeps the last result. Readiness reads that result and never
// sends a chat request: Kubernetes gives a probe one second by default and abandons it, so a model call inside
// the probe would take every pod out of service whenever the provider is slow.
public sealed class AIProviderHealthMonitor : BackgroundService
{
    public const string Pending = "Pending";
    public const string Healthy = "Healthy";

    private readonly AIConnectivityValidator _validator;
    private readonly HealthOptions _options;
    private readonly ILogger<AIProviderHealthMonitor> _logger;
    private volatile string _status = Pending;

    public AIProviderHealthMonitor(AIConnectivityValidator validator, IOptions<HealthOptions> options, ILogger<AIProviderHealthMonitor> logger)
    {
        _validator = validator;
        _options = options.Value;
        _logger = logger;
    }

    // Pending until the first probe ends, then Healthy or Unhealthy with the reason.
    public string Status => _status;

    // One probe with a deadline of its own. The background loop calls it; the chapter check calls it directly,
    // because a run with --check never starts the host and its background services.
    public async Task<string> ProbeAsync(CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(_options.AIProbeTimeoutSeconds));
        try
        {
            _status = await _validator.ValidateAsync(deadline.Token) ? Healthy : "Unhealthy";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _status = $"Unhealthy (no answer in {_options.AIProbeTimeoutSeconds} s)";
        }

        return _status;
    }

    // One short model request per interval and pod, more often while the provider fails.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var status = await ProbeAsync(stoppingToken);
                if (status != Healthy)
                {
                    _logger.LogWarning("AI provider probe: {Status}", status);
                }

                var wait = status == Healthy ? _options.AIProbeIntervalSeconds : _options.AIProbeRetrySeconds;
                await Task.Delay(TimeSpan.FromSeconds(wait), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping.
        }
    }
}
