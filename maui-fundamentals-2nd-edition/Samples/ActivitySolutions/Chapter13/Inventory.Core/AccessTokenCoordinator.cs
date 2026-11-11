namespace MauiInventory.Core;
public sealed class AccessTokenCoordinator(Func<CancellationToken, Task<AccessSession>> refresh, TimeProvider clock)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private AccessSession? current;
    private DateTimeOffset lastAttempt = DateTimeOffset.MinValue;
    public async Task<AccessSession> GetAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        {
            var now = clock.GetUtcNow();
            if (current is not null && current.ExpiresAt > now.AddMinutes(2))
                return current;
            // A short-lived but still valid result must not cause a refresh on every call.
            if (now - lastAttempt < TimeSpan.FromSeconds(30))
            {
                if (current is not null && current.ExpiresAt > now)
                    return current;
                throw new InvalidOperationException("Token renewal is temporarily unavailable.");
            }

            lastAttempt = now;
            var renewed = await refresh(token);
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(renewed.AccessToken) || renewed.ExpiresAt <= clock.GetUtcNow())
                throw new InvalidDataException("Renewal returned no usable token.");
            return current = renewed;
        }
        finally
        {
            gate.Release();
        }
    }
}
