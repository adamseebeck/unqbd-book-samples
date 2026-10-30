using WarehouseApi.Fulfillment;

namespace WarehouseApi.Tests.Unit;

public class ChannelFulfillmentJobQueueTests
{
    [Fact]
    public async Task Dequeue_ReturnsJobsInTheOrderTheyWereQueued()
    {
        var queue = new ChannelFulfillmentJobQueue();
        var first = new FulfillmentJob(Guid.NewGuid(), "first", DateTimeOffset.UtcNow);
        var second = new FulfillmentJob(Guid.NewGuid(), "second", DateTimeOffset.UtcNow);

        Assert.True(queue.TryEnqueue(first));
        Assert.True(queue.TryEnqueue(second));

        Assert.Equal(2, queue.Count);
        Assert.Equal(first, await queue.DequeueAsync());
        Assert.Equal(second, await queue.DequeueAsync());
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void TryEnqueue_AnswersAtOnce_WhenTheQueueIsFull()
    {
        var queue = new ChannelFulfillmentJobQueue();
        for (var i = 0; i < 1000; i++)
        {
            Assert.True(queue.TryEnqueue(new FulfillmentJob(Guid.NewGuid(), $"job-{i}", DateTimeOffset.UtcNow)));
        }

        // The caller does not wait for space; the shipment row stays Pending for the sweep
        Assert.False(queue.TryEnqueue(new FulfillmentJob(Guid.NewGuid(), "one too many", DateTimeOffset.UtcNow)));
        Assert.Equal(1000, queue.Count);
    }

    [Fact]
    public async Task Dequeue_StopsWaiting_WhenTheTokenIsCanceled()
    {
        var queue = new ChannelFulfillmentJobQueue();
        using var stopping = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.DequeueAsync(stopping.Token).AsTask());
    }
}
