namespace MauiInventory.Core;
public interface IMessagePublisher
{
    event EventHandler Updated;
}

public sealed class ActiveMessageSubscription(EventHandler callback) : IDisposable
{
    private IMessagePublisher? publisher;
    // Call from the owning UI lifetime, not concurrently from arbitrary threads.
    public void Attach(IMessagePublisher value)
    {
        if (ReferenceEquals(publisher, value))
            return;
        Dispose();
        publisher = value;
        publisher.Updated += callback;
    }

    public void Dispose()
    {
        if (publisher is null)
            return;
        publisher.Updated -= callback;
        publisher = null;
    }
}
