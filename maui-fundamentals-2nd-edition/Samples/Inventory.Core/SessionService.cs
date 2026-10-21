namespace MauiInventory.Core;
public sealed record AccessSession(string AccessToken, DateTimeOffset ExpiresAt);
public interface IIdentityClient
{
    Task<AccessSession> SignInAsync(CancellationToken cancellationToken);
    Task SignOutAsync(CancellationToken cancellationToken);
}

public interface ITokenVault
{
    Task SetAsync(string value);
    Task<string?> GetAsync();
    void Remove();
}

public sealed class SessionService(IIdentityClient identity, ITokenVault vault)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task<bool> SignInAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var result = await identity.SignInAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(result.AccessToken) || result.ExpiresAt <= DateTimeOffset.UtcNow)
                throw new InvalidDataException("Identity provider returned no usable access token.");
            await vault.SetAsync(result.AccessToken);
            return true;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Always remove the app's token, including when remote/cache sign-out fails.
            vault.Remove();
            await identity.SignOutAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }
}

public enum PermissionDecision
{
    Granted,
    Denied,
    Disabled,
    Unavailable
}

public readonly record struct Position(double Latitude, double Longitude);
public interface ILocationDevice
{
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

public static class InventoryMetrics
{
    public static long StockValueCents(IEnumerable<Product> products) => products.Aggregate(0L, (total, p) => checked(total + checked(p.UnitPriceCents * p.Quantity)));
    public static bool IsShake(double x, double y, double z, double threshold = 2.0) => Math.Sqrt(x * x + y * y + z * z) > threshold;
}
