using MauiInventory.Core;
using Microsoft.Identity.Client;

namespace MauiInventory.App;
// Replace these public identifiers only after registering your own Entra public client.
// A native application must never contain a client secret.
public sealed class MsalIdentityClient : IIdentityClient
{
    private IPublicClientApplication? client;
    private static readonly string[] Scopes = ["User.Read"];
    public const string ClientId = "00000000-0000-0000-0000-000000000000";
    public const string TenantId = "00000000-0000-0000-0000-000000000000";
    public const string CallbackScheme = "msal" + ClientId;
    private IPublicClientApplication Client => client ??= CreateClient();

    private static IPublicClientApplication CreateClient()
    {
#if MACCATALYST
        throw new PlatformNotSupportedException("This pinned MSAL sample has no Mac Catalyst native adapter. Use the controlled renewal exercise.");
#else
        if (!Guid.TryParse(ClientId, out var clientId) || clientId == Guid.Empty || !Guid.TryParse(TenantId, out var tenantId) || tenantId == Guid.Empty)
            throw new InvalidOperationException("Register and configure an Entra public client before signing in.");
        var redirect = DeviceInfo.Platform == DevicePlatform.WinUI ? "http://localhost" : $"msal{ClientId}://auth";
        return PublicClientApplicationBuilder.Create(ClientId).WithAuthority(AzureCloudInstance.AzurePublic, TenantId).WithRedirectUri(redirect).Build();
#endif
    }

    public async Task<AccessSession> SignInAsync(CancellationToken cancellationToken)
    {
        var account = (await Client.GetAccountsAsync()).FirstOrDefault();
        try
        {
            var silent = await Client.AcquireTokenSilent(Scopes, account).ExecuteAsync(cancellationToken);
            return new AccessSession(silent.AccessToken, silent.ExpiresOn);
        }
        catch (MsalUiRequiredException)
        {
            var interactive = Client.AcquireTokenInteractive(Scopes).WithUseEmbeddedWebView(false);
#if ANDROID
            interactive=interactive.WithParentActivityOrWindow(()=>Platform.CurrentActivity!);
#endif
            var result = await interactive.ExecuteAsync(cancellationToken);
            return new AccessSession(result.AccessToken, result.ExpiresOn);
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        if (client is null)
            return;
        foreach (var account in await client.GetAccountsAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.RemoveAsync(account);
        }
    }
}
