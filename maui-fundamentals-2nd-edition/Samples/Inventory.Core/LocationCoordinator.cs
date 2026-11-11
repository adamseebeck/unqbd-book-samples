namespace MauiInventory.Core;
public enum PermissionDecision {
    Granted,
    Denied,
    Disabled,
    Unavailable
}

public readonly record struct Position(double Latitude, double Longitude);
public interface ILocationDevice {
    Task<PermissionDecision> RequestPermissionAsync();
    Task<Position?> LocateAsync(CancellationToken cancellationToken);
}

public sealed class LocationCoordinator(ILocationDevice device)
{
    public async Task<Position?> FindAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var permission = await device.RequestPermissionAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (permission != PermissionDecision.Granted)
            return null;
        return await device.LocateAsync(cancellationToken);
    }
}
