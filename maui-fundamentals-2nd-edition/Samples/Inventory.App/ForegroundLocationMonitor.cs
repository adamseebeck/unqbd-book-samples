namespace MauiInventory.App;
public sealed class ForegroundLocationMonitor
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool ownsListening;
    private long generation;
    private EventHandler<GeolocationListeningFailedEventArgs>? failed;
    public event EventHandler? Updated;
    public string Status { get; private set; } = "Tracking stopped.";

    public async Task StartAsync(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            if (ownsListening)
                return;
            var permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            token.ThrowIfCancellationRequested();
            if (permission != PermissionStatus.Granted)
            {
                Status = "Location permission denied.";
                return;
            }

            if (Geolocation.Default.IsListeningForeground)
                throw new InvalidOperationException("Another feature owns location listening.");
            Geolocation.Default.LocationChanged += LocationChanged;
            var current = ++generation;
            failed = (sender, args) => ListeningFailed(current);
            Geolocation.Default.ListeningFailed += failed;
            try
            {
                ownsListening = await Geolocation.Default.StartListeningForegroundAsync(new GeolocationListeningRequest(GeolocationAccuracy.Medium) { MinimumTime = TimeSpan.FromSeconds(10), MinimumDistance = 25 });
                if (!ownsListening)
                    Detach();
                token.ThrowIfCancellationRequested();
                Status = ownsListening ? "Foreground location tracking started. No coordinates are uploaded." : "Location listening could not start.";
            }
            catch
            {
                if (ownsListening)
                    Geolocation.Default.StopListeningForeground();
                ownsListening = false;
                Detach();
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await gate.WaitAsync();
        try
        {
            Detach();
            generation++;
            if (ownsListening)
                Geolocation.Default.StopListeningForeground();
            ownsListening = false;
            Status = "Tracking stopped.";
        }
        finally
        {
            gate.Release();
        }
    }

    private void LocationChanged(object? sender, GeolocationLocationChangedEventArgs args)
    {
        Status = "A foreground position update was received; coordinates are not displayed or retained.";
        Updated?.Invoke(this, EventArgs.Empty);
    }

    private async void ListeningFailed(long observedGeneration)
    {
        // A native failure ends listening. Serialize ownership reset with Start/Stop
        // so the visible Start action can recover without a page round trip.
        await gate.WaitAsync();
        try
        {
            if (observedGeneration != generation)
                return;
            ownsListening = false;
            Detach();
            Status = "Location tracking stopped after a device error. You can start again.";
        }
        catch (Exception)
        {
            Status = "Location tracking is unavailable.";
        }
        finally
        {
            gate.Release();
        }

        Updated?.Invoke(this, EventArgs.Empty);
    }

    private void Detach()
    {
        Geolocation.Default.LocationChanged -= LocationChanged;
        if (failed is not null)
            Geolocation.Default.ListeningFailed -= failed;
        failed = null;
    }
}
