using MauiInventory.Core;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Inventory.Tests;
public sealed class SecurityExerciseTests
{
    [Fact]
    public void Pins_add_a_check_to_valid_tls_and_accept_current_or_backup_spki()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=inventory.example", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        var current = Convert.ToBase64String(SHA256.HashData(certificate.PublicKey.ExportSubjectPublicKeyInfo()));
        var backup = Convert.ToBase64String(new byte[32]);
        Assert.True(SpkiPinPolicy.Accept(certificate, SslPolicyErrors.None, [current, backup]));
        Assert.True(SpkiPinPolicy.Accept(certificate, SslPolicyErrors.None, [backup, current]));
        Assert.False(SpkiPinPolicy.Accept(certificate, SslPolicyErrors.RemoteCertificateChainErrors, [current, backup]));
        Assert.False(SpkiPinPolicy.Accept(certificate, SslPolicyErrors.RemoteCertificateNameMismatch, [current, backup]));
        Assert.False(SpkiPinPolicy.Accept(certificate, SslPolicyErrors.None, [backup, backup]));
        Assert.False(SpkiPinPolicy.Accept(certificate, SslPolicyErrors.None, [current]));
    // Passing None above isolates pin policy; it does not establish trust in this test certificate.
    }

    [Fact]
    public async Task Bearer_handler_refuses_other_origins_before_acquiring_or_sending_credentials()
    {
        int acquisitions = 0;
        var transport = new Transport();
        using var client = new HttpClient(new BearerSessionHandler(new Uri("https://inventory.example"), _ =>
        {
            acquisitions++;
            return Task.FromResult(new AccessSession("local-test", DateTimeOffset.UtcNow.AddHours(1)));
        }, TimeProvider.System) { InnerHandler = transport });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("https://other.example/catalog"));
        Assert.Equal(0, acquisitions);
        Assert.Equal(0, transport.Calls);
        using var response = await client.GetAsync("https://inventory.example/catalog");
        Assert.Equal(1, acquisitions);
        Assert.Equal("Bearer local-test", transport.Authorization);
    }

    [Fact]
    public async Task Denied_local_unlock_never_reveals_and_canceled_unlock_cannot_succeed_late()
    {
        Assert.False(await new LocalPrivacyGate(new Unlock(false)).RevealAsync());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new LocalPrivacyGate(new Unlock(true)).RevealAsync(cancellation.Token));
    }

    private sealed class Transport : HttpMessageHandler
    {
        public int Calls;
        public string? Authorization;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class Unlock(bool value) : ILocalUnlock
    {
        public Task<bool> AuthenticateAsync(CancellationToken token) => Task.FromResult(value);
    }
}
