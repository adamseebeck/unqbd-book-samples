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
