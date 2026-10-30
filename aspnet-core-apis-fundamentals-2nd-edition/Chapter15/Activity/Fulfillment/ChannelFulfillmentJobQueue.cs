using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace WarehouseApi.Fulfillment;

public class ChannelFulfillmentJobQueue : IFulfillmentJobQueue
{
    private readonly Channel<FulfillmentJob> _channel;

    // The capacity and the behavior of a full queue come from Fulfillment:Queue in appsettings.json
    public ChannelFulfillmentJobQueue(IOptions<FulfillmentQueueOptions> options)
    {
        _channel = Channel.CreateBounded<FulfillmentJob>(new BoundedChannelOptions(options.Value.Capacity)
        {
            FullMode = options.Value.FullMode,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public int Count => _channel.Reader.Count;

    // TryWrite answers at once; a full queue returns false instead of holding the request that queues the job
    public bool TryEnqueue(FulfillmentJob job) => _channel.Writer.TryWrite(job);

    // Waits for the next job; a canceled token ends the wait with OperationCanceledException
    public ValueTask<FulfillmentJob> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
