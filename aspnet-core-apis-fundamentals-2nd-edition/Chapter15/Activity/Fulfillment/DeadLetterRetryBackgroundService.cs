using Microsoft.Extensions.Options;

namespace WarehouseApi.Fulfillment;

// Sends failed jobs back to the main queue when their retry is due
public class DeadLetterRetryBackgroundService : BackgroundService
{
    private readonly IFulfillmentJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DeadLetterOptions _options;
    private readonly ILogger<DeadLetterRetryBackgroundService> _logger;

    public DeadLetterRetryBackgroundService(
        IFulfillmentJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<DeadLetterOptions> options,
        ILogger<DeadLetterRetryBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.RetryIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RetryDueJobsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown: failed jobs stay in the dead-letter queue until the next start
        }
    }

    private async Task RetryDueJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var deadLetters = scope.ServiceProvider.GetRequiredService<IFulfillmentDeadLetterQueue>();

            foreach (var failed in await deadLetters.GetPendingAsync(_options.BatchSize, stoppingToken))
            {
                if (await deadLetters.MarkForRetryAsync(failed.OrderId, stoppingToken))
                {
                    // Back in the main queue: the processor's attempts and the resilience handler take over. The row
                    // is Pending again, so the sweep sends it if the queue is full
                    _queue.TryEnqueue(new FulfillmentJob(failed.OrderId, failed.CorrelationId, DateTimeOffset.UtcNow));
                    _logger.LogInformation(
                        "Retrying the shipment request for order {OrderId}, failed at {FailedAt}", failed.OrderId, failed.FailedAt);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed sweep must not stop the application; the next tick tries again
            _logger.LogError(ex, "The dead-letter retry sweep failed");
        }
    }
}
