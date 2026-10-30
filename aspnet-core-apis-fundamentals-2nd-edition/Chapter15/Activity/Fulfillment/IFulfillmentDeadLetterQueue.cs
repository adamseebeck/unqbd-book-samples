using WarehouseApi.Contracts;

namespace WarehouseApi.Fulfillment;

public interface IFulfillmentDeadLetterQueue
{
    Task StoreAsync(FailedFulfillmentJob job, CancellationToken cancellationToken = default);

    // Failed jobs whose next retry is due, oldest first
    Task<IReadOnlyList<FailedFulfillmentJob>> GetPendingAsync(int count, CancellationToken cancellationToken = default);

    // Moves a failed job back to Pending; false when it is no longer in the dead-letter queue
    Task<bool> MarkForRetryAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<DeadLetterStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
}
