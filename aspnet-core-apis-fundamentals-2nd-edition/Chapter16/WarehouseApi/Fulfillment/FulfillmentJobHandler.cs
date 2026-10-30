using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using WarehouseApi.Contracts;
using WarehouseApi.Integration;
using WarehouseApi.Models;
using WarehouseApi.Persistence;

namespace WarehouseApi.Fulfillment;

// The work for one job. A Scoped service: every job gets its own DbContext
public class FulfillmentJobHandler
{
    private readonly WarehouseDbContext _context;
    private readonly IExternalShipmentService _shipmentService;
    private readonly FulfillmentOptions _options;
    private readonly ILogger<FulfillmentJobHandler> _logger;

    public FulfillmentJobHandler(
        WarehouseDbContext context,
        IExternalShipmentService shipmentService,
        IOptions<FulfillmentOptions> options,
        ILogger<FulfillmentJobHandler> logger)
    {
        _context = context;
        _shipmentService = shipmentService;
        _options = options.Value;
        _logger = logger;
    }

    // stoppingToken ends the waits between attempts when shutdown starts; workToken cancels the
    // ERP call and the database work only when the host stops waiting for the job
    public async Task HandleAsync(FulfillmentJob job, CancellationToken stoppingToken, CancellationToken workToken)
    {
        var order = await _context.Orders.AsNoTracking().FirstAsync(o => o.Id == job.OrderId, workToken);
        var delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds);

        for (var attempt = 1; ; attempt++)
        {
            // Counted before the call, so an attempt counts even when the process stops during it. A shipment
            // that is no longer Pending (registered by an earlier job, cancelled with its order) is not sent
            if (!await UpdatePendingAsync(job.OrderId, set => set.SetProperty(s => s.Attempts, s => s.Attempts + 1), workToken))
            {
                _logger.LogInformation("The shipment for order {OrderId} is no longer pending; nothing to send", job.OrderId);
                return;
            }

            try
            {
                var confirmation = await _shipmentService.CreateShipmentAsync(
                    order.Id, order.FulfillmentWarehouseCode!, workToken);

                var registered = await UpdatePendingAsync(job.OrderId, set => set
                    .SetProperty(s => s.Status, ShipmentStatus.Registered)
                    .SetProperty(s => s.ShipmentId, confirmation.ShipmentId)
                    .SetProperty(s => s.TrackingNumber, confirmation.TrackingNumber)
                    .SetProperty(s => s.RegisteredAt, confirmation.ConfirmedAt.UtcDateTime)
                    .SetProperty(s => s.LastError, (string?)null), workToken);

                if (registered)
                {
                    _logger.LogInformation(
                        "Registered shipment {ShipmentId} for order {OrderId} on attempt {Attempt}, {QueuedMs} ms after it was queued, correlation ID {CorrelationId}",
                        confirmation.ShipmentId, job.OrderId, attempt,
                        (long)(DateTimeOffset.UtcNow - job.EnqueuedAt).TotalMilliseconds, job.CorrelationId);
                }
                else if (await _context.Shipments.AnyAsync(s => s.OrderId == job.OrderId && s.Status == ShipmentStatus.Cancelled, workToken))
                {
                    // The order was cancelled during the call; the API cannot withdraw the ERP's shipment
                    _logger.LogError(
                        "Order {OrderId} was cancelled while the ERP created shipment {ShipmentId}; cancel that shipment in the ERP",
                        job.OrderId, confirmation.ShipmentId);
                }

                return;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < _options.MaxAttempts)
            {
                // Saved before the wait, so the status monitor shows this error while the job waits
                await UpdatePendingAsync(job.OrderId, set => set.SetProperty(s => s.LastError, Truncate(ex.Message)), workToken);

                _logger.LogWarning(ex,
                    "Attempt {Attempt} of {MaxAttempts} to register the shipment for order {OrderId} failed; next attempt in {Delay}",
                    attempt, _options.MaxAttempts, job.OrderId, delay);

                // Shutdown ends this wait at once: the shipment stays Pending, and the next sweep or start sends it
                await Task.Delay(delay, stoppingToken);
                delay *= 2;
            }
        }
    }

    // Called by the processor after the last attempt has failed
    public Task MarkFailedAsync(FulfillmentJob job, Exception failure, CancellationToken workToken) =>
        UpdatePendingAsync(job.OrderId, set => set
            .SetProperty(s => s.Status, ShipmentStatus.Failed)
            .SetProperty(s => s.LastError, Truncate(failure.Message)), workToken);

    // One UPDATE that applies only to a Pending shipment: a cancellation or a faster job is never overwritten
    private async Task<bool> UpdatePendingAsync(
        Guid orderId, Action<UpdateSettersBuilder<Shipment>> setters, CancellationToken cancellationToken) =>
        await _context.Shipments
            .Where(s => s.OrderId == orderId && s.Status == ShipmentStatus.Pending)
            .ExecuteUpdateAsync(setters, cancellationToken) == 1;

    // Failures that a later attempt can fix. The resilience handler has already retried each call,
    // so these remain when the ERP stays unavailable for longer
    private static bool IsTransient(Exception exception) => exception is
        HttpRequestException { StatusCode: null or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError }
        or BrokenCircuitException
        or TimeoutRejectedException;

    private static string Truncate(string message) =>
        message.Length <= Shipment.MaxErrorLength ? message : message[..Shipment.MaxErrorLength];
}
