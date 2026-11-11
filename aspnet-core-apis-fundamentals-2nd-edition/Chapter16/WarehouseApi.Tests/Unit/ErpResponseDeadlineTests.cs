using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WarehouseApi.Integration;
using WarehouseApi.Services;

namespace WarehouseApi.Tests.Unit;

public class ErpResponseDeadlineTests
{
    [Fact]
    public async Task StalledBody_HasACompleteResponseDeadline()
    {
        using var client = new HttpClient(new HeaderOnlyHandler())
        {
            BaseAddress = new Uri("http://127.0.0.1/"),
            Timeout = Timeout.InfiniteTimeSpan,
        };
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<TimeoutException>(() =>
            Create(client).CreateShipmentAsync(Guid.NewGuid(), "SEA", guard.Token)
        );
        Assert.False(guard.IsCancellationRequested);
    }

    [Fact]
    public async Task StalledBody_PreservesCallerCancellation()
    {
        using var client = new HttpClient(new HeaderOnlyHandler())
        {
            BaseAddress = new Uri("http://127.0.0.1/"),
            Timeout = Timeout.InfiniteTimeSpan,
        };
        using var caller = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Create(client).CreateShipmentAsync(Guid.NewGuid(), "SEA", caller.Token)
        );
        Assert.True(caller.IsCancellationRequested);
    }

    [Fact]
    public async Task CompleteBody_StillDecodesTheConfirmation()
    {
        using var client = new HttpClient(new CompleteHandler())
        {
            BaseAddress = new Uri("http://127.0.0.1/"),
            Timeout = Timeout.InfiniteTimeSpan,
        };
        var result = await Create(client).CreateShipmentAsync(Guid.NewGuid(), "SEA");
        Assert.Equal("test-shipment", result.ShipmentId);
        Assert.Equal("test-tracking", result.TrackingNumber);
    }

    private static ExternalShipmentService Create(HttpClient client)
    {
        var correlation = new CorrelationContext();
        correlation.SetCorrelationId("deadline-test");
        return new ExternalShipmentService(
            client,
            correlation,
            Options.Create(
                new ExternalShipmentServiceOptions
                {
                    BaseUrl = "http://127.0.0.1/",
                    ApiKey = "synthetic",
                    CompleteResponseTimeoutSeconds = 1,
                }
            ),
            NullLogger<ExternalShipmentService>.Instance
        );
    }

    private sealed class HeaderOnlyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StalledContent() }
            );
    }

    private sealed class StalledContent : HttpContent
    {
        public StalledContent() =>
            Headers.ContentType = new MediaTypeHeaderValue("application/json");

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw new NotSupportedException("Use the cancellation-aware overload.");

        protected override async Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken
        ) => await Task.Delay(Timeout.Infinite, cancellationToken);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class CompleteHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"shipmentId":"test-shipment","confirmedAt":"2026-11-09T00:00:00Z","trackingNumber":"test-tracking"}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                }
            );
    }
}
