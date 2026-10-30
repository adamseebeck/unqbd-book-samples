namespace WarehouseApi.Fulfillment;

public interface IFulfillmentJobQueue
{
    // The number of jobs waiting to be processed
    int Count { get; }

    // Never waits: false when the queue is full. The shipment row stays Pending, and the sweep sends it
    bool TryEnqueue(FulfillmentJob job);
    ValueTask<FulfillmentJob> DequeueAsync(CancellationToken cancellationToken = default);
}
