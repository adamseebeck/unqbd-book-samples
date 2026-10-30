using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WarehouseApi.Contracts;
using WarehouseApi.Persistence;
using WarehouseApi.Services;
using WarehouseApi.Telemetry;

namespace WarehouseApi.Fulfillment;

public class FulfillmentProcessorBackgroundService : BackgroundService
{
    // A sweep sends at most this many shipments; the next sweep takes the rest
    private const int SweepBatchSize = 50;

    private readonly IFulfillmentJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _sweepInterval;
    private readonly ILogger<FulfillmentProcessorBackgroundService> _logger;

    // Canceled when the host's shutdown timeout runs out, not when shutdown starts
    private readonly CancellationTokenSource _shutdownDeadline = new();

    public FulfillmentProcessorBackgroundService(
        IFulfillmentJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<FulfillmentOptions> options,
        ILogger<FulfillmentProcessorBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _sweepInterval = TimeSpan.FromSeconds(options.Value.SweepIntervalSeconds);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Shipments that were still waiting when the application last stopped
            await SweepAsync(DateTime.MaxValue, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // A database outage at startup must not stop the host: the rows stay Pending for the periodic sweep
            _logger.LogError(ex, "The startup sweep for pending shipment requests failed");
        }

        var nextSweep = DateTime.UtcNow + _sweepInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait for the next job, but at most one sweep interval
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            wait.CancelAfter(_sweepInterval);
            try
            {
                // The job in progress continues after shutdown starts; only the deadline cancels it
                await ProcessAsync(await _queue.DequeueAsync(wait.Token), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // Shutdown started while the processor waited: take no new job
            }
            catch (OperationCanceledException)
            {
                // No job within the interval
            }

            if (DateTime.UtcNow >= nextSweep)
            {
                try
                {
                    // Rows that no job carries, requested more than one interval ago: queued jobs come first
                    await SweepAsync(DateTime.UtcNow - _sweepInterval, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // A database outage must not stop the host; the next sweep tries again
                    _logger.LogError(ex, "The sweep for pending shipment requests failed");
                }

                nextSweep = DateTime.UtcNow + _sweepInterval;
            }
        }

        _logger.LogInformation(
            "Fulfillment processor stopped; {QueuedJobs} queued job(s) stay pending until the next start", _queue.Count);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fulfillment processor stopping; the job in progress can finish");

        // Returns when ExecuteAsync has finished, or when the host stops waiting: the host cancels this
        // token once HostOptions.ShutdownTimeout runs out, and then the job in progress is canceled
        await base.StopAsync(cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            _shutdownDeadline.Cancel();
        }
    }

    public override void Dispose()
    {
        _shutdownDeadline.Dispose();
        base.Dispose();
    }

    // The outbox relay: sends Pending rows from the table, oldest first, one job at a time
    private async Task SweepAsync(DateTime requestedBefore, CancellationToken stoppingToken)
    {
        List<FulfillmentJob> pending;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            var rows = await context.Shipments
                .AsNoTracking()
                .Where(s => s.Status == ShipmentStatus.Pending && s.RequestedAt < requestedBefore)
                .OrderBy(s => s.RequestedAt)
                .Take(SweepBatchSize)
                .Select(s => new { s.OrderId, s.CorrelationId })
                .ToListAsync(stoppingToken);

            pending = rows.Select(r => new FulfillmentJob(r.OrderId, r.CorrelationId, DateTimeOffset.UtcNow)).ToList();
        }

        if (pending.Count > 0)
        {
            _logger.LogInformation("Resuming {Count} pending shipment request(s)", pending.Count);
        }

        foreach (var job in pending.TakeWhile(_ => !stoppingToken.IsCancellationRequested))
        {
            await ProcessAsync(job, stoppingToken);
        }
    }

    private async Task ProcessAsync(FulfillmentJob job, CancellationToken stoppingToken)
    {
        // No request scope exists here: each job gets its own scope, DbContext and correlation context
        await using var scope = _scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ICorrelationContext>().SetCorrelationId(job.CorrelationId);

        // Every entry written while the job runs carries the correlation ID of the request that queued it
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = job.CorrelationId,
            ["OrderId"] = job.OrderId
        });

        using var activity = WarehouseTelemetry.Source.StartActivity(WarehouseTelemetry.ActivityNames.RegisterShipment);
        activity?.SetTag(WarehouseTelemetry.Tags.OrderId, job.OrderId.ToString());
        activity?.SetTag(WarehouseTelemetry.Tags.CorrelationId, job.CorrelationId);

        try
        {
            var handler = scope.ServiceProvider.GetRequiredService<FulfillmentJobHandler>();
            await handler.HandleAsync(job, stoppingToken, _shutdownDeadline.Token);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Shutdown interrupted the shipment request for order {OrderId}; it stays pending", job.OrderId);
        }
        catch (Exception ex)
        {
            // Every job failure ends here: an exception that leaves ExecuteAsync would stop the whole application
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex,
                "The shipment request for order {OrderId} failed, correlation ID {CorrelationId}", job.OrderId, job.CorrelationId);
            await MarkFailedAsync(scope.ServiceProvider, job, ex);
        }
    }

    private async Task MarkFailedAsync(IServiceProvider services, FulfillmentJob job, Exception failure)
    {
        try
        {
            await services.GetRequiredService<FulfillmentJobHandler>().MarkFailedAsync(job, failure, _shutdownDeadline.Token);
        }
        catch (Exception ex)
        {
            // The shipment stays Pending, so the next sweep sends it again
            _logger.LogError(ex, "Could not record the failed shipment request for order {OrderId}", job.OrderId);
        }
    }
}
