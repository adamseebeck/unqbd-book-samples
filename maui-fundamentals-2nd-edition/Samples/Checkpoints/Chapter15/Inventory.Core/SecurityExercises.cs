using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MauiInventory.Core;
public static class SpkiPinPolicy
{
    public static bool Accept(X509Certificate2? certificate, SslPolicyErrors errors, IReadOnlyList<string> base64Pins)
    {
        if (certificate is null || errors != SslPolicyErrors.None || base64Pins.Count < 2)
            return false;
        byte[] publicKey = certificate.PublicKey.ExportSubjectPublicKeyInfo();
        var digest = SHA256.HashData(publicKey);
        foreach (var pin in base64Pins)
        {
            byte[] expected;
            try
            {
                expected = Convert.FromBase64String(pin);
            }
            catch (FormatException)
            {
                continue;
            }

            if (expected.Length == 32 && CryptographicOperations.FixedTimeEquals(digest, expected))
                return true;
        }

        return false;
    }
}

public sealed class BearerSessionHandler(Uri allowedOrigin, Func<CancellationToken, Task<AccessSession>> acquire, TimeProvider clock) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (allowedOrigin.Scheme != Uri.UriSchemeHttps || request.RequestUri is not { IsAbsoluteUri: true } uri || uri.Scheme != Uri.UriSchemeHttps || uri.Host != allowedOrigin.Host || uri.Port != allowedOrigin.Port)
            throw new InvalidOperationException("Credentials are restricted to the configured HTTPS origin.");
        var session = await acquire(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(session.AccessToken) || session.ExpiresAt <= clock.GetUtcNow())
            throw new InvalidDataException("A usable session is required.");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await base.SendAsync(request, token).ConfigureAwait(false);
    }
}

public interface ILocalUnlock
{
    Task<bool> AuthenticateAsync(CancellationToken token);
}

public sealed class LocalPrivacyGate(ILocalUnlock device)
{
    public async Task<bool> RevealAsync(CancellationToken token = default)
    {
        var accepted = await device.AuthenticateAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return accepted;
    // This boolean only gates local presentation. It is not a server access token,
    // and does not cryptographically bind SecureStorage reads to a biometric key.
    }
}
