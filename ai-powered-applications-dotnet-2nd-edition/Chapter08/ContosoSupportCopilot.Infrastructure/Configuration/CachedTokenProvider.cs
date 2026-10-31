using System.ClientModel;
using System.ClientModel.Primitives;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

// BearerTokenPolicy asks its provider for a token before every
// request, and AzureCliCredential runs the Azure CLI each time it
// is asked. This provider keeps the token until the credential's
// refresh time, or five minutes before it expires. Give each policy
// its own provider, so that each provider serves one scope.
public sealed class CachedTokenProvider(
    AuthenticationTokenProvider inner) : AuthenticationTokenProvider
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile AuthenticationToken? _token;

    public override GetTokenOptions? CreateTokenOptions(
        IReadOnlyDictionary<string, object> properties) =>
        inner.CreateTokenOptions(properties);

    public override AuthenticationToken GetToken(
        GetTokenOptions options, CancellationToken cancellationToken)
    {
        if (Fresh() is { } cached)
        {
            return cached;
        }

        _gate.Wait(cancellationToken);
        try
        {
            var token = Fresh()
                ?? inner.GetToken(options, cancellationToken);
            _token = token;
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    public override ValueTask<AuthenticationToken> GetTokenAsync(
        GetTokenOptions options, CancellationToken cancellationToken)
        => Fresh() is { } cached
            ? ValueTask.FromResult(cached)
            : RenewAsync(options, cancellationToken);

    // One caller renews the token; the others wait for it.
    private async ValueTask<AuthenticationToken> RenewAsync(
        GetTokenOptions options, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var token = Fresh() ?? await inner.GetTokenAsync(
                options, cancellationToken);
            _token = token;
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private AuthenticationToken? Fresh()
    {
        var token = _token;
        var renewAt = token?.RefreshOn
            ?? token?.ExpiresOn?.AddMinutes(-5);
        return renewAt > DateTimeOffset.UtcNow ? token : null;
    }
}
