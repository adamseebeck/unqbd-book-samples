using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WarehouseApi.Contracts;
using WarehouseApi.Models;
using WarehouseApi.Persistence;

namespace WarehouseApi.Fulfillment;

// The dead-letter queue is the set of Failed shipments, so it survives restarts like the shipments themselves
public class ShipmentDeadLetterQueue : IFulfillmentDeadLetterQueue
{
    private readonly WarehouseDbContext _context;
    private readonly DeadLetterOptions _options;

    public ShipmentDeadLetterQueue(WarehouseDbContext context, IOptions<DeadLetterOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task StoreAsync(FailedFulfillmentJob job, CancellationToken cancellationToken = default)
    {
        // The number of earlier retries sets the backoff: 1, 2, 4, 8... times the base delay
        var retries = await _context.Shipments
            .Where(s => s.OrderId == job.OrderId)
            .Select(s => (int?)s.DeadLetterRetries)
            .FirstOrDefaultAsync(cancellationToken);
        if (retries is null)
        {
            return;
        }

        var failedAt = job.FailedAt.UtcDateTime;
        var error = job.Error.Length <= Shipment.MaxErrorLength ? job.Error : job.Error[..Shipment.MaxErrorLength];
        var nextRetryAt = failedAt.AddSeconds(_options.BaseRetryDelaySeconds * Math.Pow(2, retries.Value));

        // One conditional UPDATE: only a Pending shipment fails. A job that registered it, or a cancellation
        // that cancelled it, is never overwritten, as a read followed by a save could do
        await _context.Shipments
            .Where(s => s.OrderId == job.OrderId && s.Status == ShipmentStatus.Pending)
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.Status, ShipmentStatus.Failed)
                .SetProperty(s => s.LastError, error)
                .SetProperty(s => s.DeadLetteredAt, failedAt)
                .SetProperty(s => s.NextRetryAt, nextRetryAt), cancellationToken);
    }

    public async Task<IReadOnlyList<FailedFulfillmentJob>> GetPendingAsync(int count, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var due = await _context.Shipments
            .AsNoTracking()
            .Where(s => s.Status == ShipmentStatus.Failed && s.NextRetryAt <= now && s.DeadLetterRetries < _options.MaxRetries)
            .OrderBy(s => s.DeadLetteredAt)
            .Take(count)
            .Select(s => new { s.OrderId, s.CorrelationId, s.LastError, s.DeadLetteredAt })
            .ToListAsync(cancellationToken);

        return due
            .Select(s => new FailedFulfillmentJob(
                s.OrderId, s.CorrelationId, s.LastError ?? string.Empty,
                new DateTimeOffset(DateTime.SpecifyKind(s.DeadLetteredAt!.Value, DateTimeKind.Utc))))
            .ToList();
    }

    public async Task<bool> MarkForRetryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // One conditional UPDATE: a Failed shipment goes back to Pending only while its order is not cancelled.
        // A cancellation turns a Failed shipment into Cancelled, so a cancelled order's shipment is never sent
        var retriedAt = DateTime.UtcNow;
        var updated = await _context.Shipments
            .Where(s => s.OrderId == orderId && s.Status == ShipmentStatus.Failed
                && _context.Orders.Any(o => o.Id == orderId && o.Status != OrderStatus.Cancelled))
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.Status, ShipmentStatus.Pending)
                .SetProperty(s => s.DeadLetterRetries, s => s.DeadLetterRetries + 1)
                .SetProperty(s => s.LastRetriedAt, retriedAt), cancellationToken);

        return updated == 1;
    }

    public async Task<DeadLetterStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var failed = _context.Shipments.AsNoTracking().Where(s => s.Status == ShipmentStatus.Failed);
        var depth = await failed.CountAsync(cancellationToken);
        var oldest = await failed.MinAsync(s => s.DeadLetteredAt, cancellationToken);

        var hourAgo = DateTime.UtcNow.AddHours(-1);
        var retried = _context.Shipments.AsNoTracking().Where(s => s.LastRetriedAt >= hourAgo);
        var retries = await retried.CountAsync(cancellationToken);
        var succeeded = await retried.CountAsync(s => s.Status == ShipmentStatus.Registered, cancellationToken);

        return new DeadLetterStatusResponse(
            depth,
            oldest,
            retries,
            succeeded,
            retries == 0 ? null : (double)succeeded / retries);
    }
}
