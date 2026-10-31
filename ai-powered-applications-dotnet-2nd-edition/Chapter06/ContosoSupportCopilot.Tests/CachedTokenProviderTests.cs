using System.ClientModel;
using System.ClientModel.Primitives;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class CachedTokenProviderTests
{
    private static readonly Dictionary<string, object> Scope = new() { ["scopes"] = new[] { "https://ai.azure.com/.default" } };

    [Fact]
    public async Task GetTokenAsync_AsksTheCredentialOnce_WhileTheTokenIsFresh()
    {
        var credential = new CountingCredential(TimeSpan.FromHours(1));
        var provider = new CachedTokenProvider(credential);
        var options = provider.CreateTokenOptions(Scope)!;

        var first = await provider.GetTokenAsync(options, CancellationToken.None);
        var second = await provider.GetTokenAsync(options, CancellationToken.None);
        var third = provider.GetToken(options, CancellationToken.None);

        Assert.Equal(1, credential.Calls);
        Assert.Same(first, second);
        Assert.Same(first, third);
    }

    [Fact]
    public async Task GetTokenAsync_AsksAgain_WithinFiveMinutesOfExpiry()
    {
        var credential = new CountingCredential(TimeSpan.FromMinutes(4));
        var provider = new CachedTokenProvider(credential);
        var options = provider.CreateTokenOptions(Scope)!;

        await provider.GetTokenAsync(options, CancellationToken.None);
        await provider.GetTokenAsync(options, CancellationToken.None);

        Assert.Equal(2, credential.Calls);
    }

    [Fact]
    public async Task GetTokenAsync_AsksOnce_ForRequestsThatArriveTogether()
    {
        var credential = new CountingCredential(TimeSpan.FromHours(1), delay: TimeSpan.FromMilliseconds(100));
        var provider = new CachedTokenProvider(credential);
        var options = provider.CreateTokenOptions(Scope)!;

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => provider.GetTokenAsync(options, CancellationToken.None).AsTask()));

        Assert.Equal(1, credential.Calls);
    }

    // A credential that counts its calls and issues tokens with a fixed lifetime.
    private sealed class CountingCredential(TimeSpan lifetime, TimeSpan delay = default) : AuthenticationTokenProvider
    {
        private int _calls;

        public int Calls => _calls;

        public override GetTokenOptions? CreateTokenOptions(IReadOnlyDictionary<string, object> properties) => new(properties);

        public override AuthenticationToken GetToken(GetTokenOptions options, CancellationToken cancellationToken) =>
            GetTokenAsync(options, cancellationToken).AsTask().GetAwaiter().GetResult();

        public override async ValueTask<AuthenticationToken> GetTokenAsync(GetTokenOptions options, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            await Task.Delay(delay, cancellationToken);
            return new AuthenticationToken($"token-{_calls}", "Bearer", DateTimeOffset.UtcNow + lifetime);
        }
    }
}
