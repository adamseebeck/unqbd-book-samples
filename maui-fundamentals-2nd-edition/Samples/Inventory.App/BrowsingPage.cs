using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class WeakMessageService : IMessagePublisher
{
    private readonly WeakEventManager manager = new();
    public event EventHandler Updated { add => manager.AddEventHandler(value); remove => manager.RemoveEventHandler(value); }

    public void Publish() => manager.HandleEvent(this, EventArgs.Empty, nameof(Updated));
}

public sealed class BrowsingPage : InventoryDashboardPage
{
    private readonly WeakMessageService messages;
    private readonly ActiveMessageSubscription subscription;
    private int callbacks;
    public BrowsingPage(WeakMessageService messages)
    {
        this.messages = messages;
        subscription = new(OnMessage);
        var send = new ToolbarItem
        {
            Text = "Send message"
        };
        send.Clicked += (_, _) => messages.Publish();
        ToolbarItems.Add(send);
        var thumbnail = new ToolbarItem
        {
            Text = "Decode local thumbnail"
        };
        thumbnail.Clicked += async (_, _) =>
        {
            thumbnail.IsEnabled = false;
            try
            {
                var bytes = await new ThumbnailCache(2_000_000).GetAsync(new("sample-thumbnail", 128, 128), ThumbnailDecoder.DecodePublicAssetAsync);
                await DisplayAlertAsync("Thumbnail", $"Resized local PNG: {bytes.Length} encoded bytes. This is not a startup benchmark.", "OK");
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Thumbnail unavailable", "Check native image support for this target.", "OK");
            }
            finally
            {
                thumbnail.IsEnabled = true;
            }
        };
        ToolbarItems.Add(thumbnail);
    }

    protected override void OnAppearing()
    {
        subscription.Attach(messages);
        base.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        subscription.Dispose();
        base.OnDisappearing();
    }

    private void OnMessage(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(() => Title = $"Browser callbacks: {++callbacks}");
}
