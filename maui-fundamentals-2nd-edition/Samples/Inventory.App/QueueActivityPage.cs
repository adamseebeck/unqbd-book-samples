using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class QueueActivityPage : ContentPage
{
    private readonly SqliteSyncQueue queue;
    private readonly SimulatedTransport transport = new();
    private readonly ConnectivitySyncCoordinator coordinator;
    private readonly IConnectivityStatus connectivity;
    private readonly Label status = new();
    private readonly ToolbarItem process = new()
    {
        Text = "Sync"
    };
    private CancellationTokenSource? appearance;
    public QueueActivityPage(SqliteSyncQueue queue, IConnectivityStatus connectivity)
    {
        this.queue = queue;
        this.connectivity = connectivity;
        coordinator = new(connectivity, new(queue, transport, TimeProvider.System));
        Title = "Durable offline queue";
        var enqueue = new Button
        {
            Text = "Enqueue a newer local revision"
        };
        enqueue.Clicked += async (_, _) => await RunAsync(async token =>
        {
            await queue.EnqueueNextAsync(1, "Fictional inventory edit", token);
        });
        var failure = new Switch
        {
            IsToggled = true
        };
        SemanticProperties.SetDescription(failure, "Simulate transient failures");
        failure.Toggled += (_, e) => transport.Fail = e.Value;
        process.Clicked += async (_, _) => await RunAsync(async token =>
        {
            await coordinator.SyncAsync(token);
        });
        var retry = new Button
        {
            Text = "Deliberately retry exhausted revisions"
        };
        retry.Clicked += async (_, _) => await RunAsync(async token =>
        {
            foreach (var row in await queue.SnapshotAsync(token))
                if (row.RetryCount >= 3)
                    await queue.RetryExhaustedAsync(row.ProductId, row.Revision, token);
        });
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "All synchronization is simulated in this process. No API write occurs."
                },
                enqueue,
                new Label
                {
                    Text = "Simulate transient failures"
                },
                failure,
                retry,
                status,
                new Label
                {
                    Text = "The queue survives restart. Three total attempts wait 1 and 2 seconds before the next try. The third failure is exhausted; its recorded 4-second due value does not schedule a fourth attempt. Recovery is explicit. Connectivity only permits a simulated attempt."
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        appearance?.Dispose();
        appearance = new();
        connectivity.Changed += ConnectivityChanged;
        await RunAsync(_ => Task.CompletedTask);
    }

    protected override void OnDisappearing()
    {
        connectivity.Changed -= ConnectivityChanged;
        appearance?.Cancel();
        base.OnDisappearing();
    }

    private void ConnectivityChanged(object? sender, EventArgs args) => MainThread.BeginInvokeOnMainThread(async () => await RunAsync(_ => Task.CompletedTask));
    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (appearance is null)
            return;
        var token = appearance.Token;
        try
        {
            await action(token);
            var rows = await queue.SnapshotAsync(token);
            var count = rows.Count;
            if (token.IsCancellationRequested)
                return;
            status.Text = $"Network permits attempt: {connectivity.CanAttempt}. Pending: {count}. Simulated sends: {transport.Calls}.\n" + string.Join("\n", rows.Select(row => $"Product {row.ProductId}, revision {row.Revision}: failures {row.RetryCount}/3; {(row.RetryCount >= 3 ? "exhausted" : "due " + row.NextAttempt.ToString("O"))}; {row.ErrorMessage}"));
            process.Text = $"Sync ({count})";
            process.IsEnabled = connectivity.CanAttempt;
            if (count > 0 && !ToolbarItems.Contains(process))
                ToolbarItems.Add(process);
            else if (count == 0)
                ToolbarItems.Remove(process);
        }
        catch (OperationCanceledException)when (token.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!token.IsCancellationRequested)
                status.Text = "Could not update the local queue.";
        }
    }

    private sealed class SimulatedTransport : ISyncTransport
    {
        public bool Fail = true;
        public int Calls;
        public async Task SendAsync(PendingSyncItem item, CancellationToken token)
        {
            Calls++;
            await Task.Delay(500, token);
            if (Fail)
                throw new HttpRequestException("Controlled simulated failure");
        }
    }
}
