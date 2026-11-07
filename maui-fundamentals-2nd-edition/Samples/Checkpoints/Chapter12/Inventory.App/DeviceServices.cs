using MauiInventory.Core;

namespace MauiInventory.App;

public sealed class MauiLocationDevice : ILocationDevice
{
    public async Task<PermissionDecision> RequestPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status == PermissionStatus.Granted ? PermissionDecision.Granted : PermissionDecision.Denied;
        }
        catch (FeatureNotSupportedException)
        {
            return PermissionDecision.Unavailable;
        }
    }

    public async Task<Position?> LocateAsync(CancellationToken cancellationToken)
    {
        var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)), cancellationToken);
        return location is null ? null : new Position(location.Latitude, location.Longitude);
    }
}
