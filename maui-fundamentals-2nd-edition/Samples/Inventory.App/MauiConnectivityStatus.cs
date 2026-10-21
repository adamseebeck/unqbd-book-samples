using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class MauiConnectivityStatus : IConnectivityStatus, IDisposable
{
    public MauiConnectivityStatus() => Connectivity.Current.ConnectivityChanged += OnChanged;
    public bool CanAttempt => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler? Changed;
    private void OnChanged(object? sender, ConnectivityChangedEventArgs args) => Changed?.Invoke(this, EventArgs.Empty);
    public void Dispose() => Connectivity.Current.ConnectivityChanged -= OnChanged;
}
