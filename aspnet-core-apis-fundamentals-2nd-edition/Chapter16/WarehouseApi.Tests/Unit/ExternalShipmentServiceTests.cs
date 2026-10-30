using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using WarehouseApi.Integration;
using WarehouseApi.Services;

namespace WarehouseApi.Tests.Unit;

// The typed client with a hand-written HttpMessageHandler: no network, no host
public class ExternalShipmentServiceTests
{
    private static readonly Guid OrderId = Guid.Parse("3f2b7c1e-5d4a-4e8f-9b0c-1a2b3c4d5e6f");

    [Fact]
    public async Task CreateShipment_PostsTheOrderWithAnIdempotencyKey()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created, new { shipmentId = "SHP-1", confirmedAt = DateTimeOffset.UtcNow, trackingNumber = "TRK-1" });
        var service = CreateService(handler);

        var confirmation = await service.CreateShipmentAsync(OrderId, "WH-001");

        Assert.Equal("SHP-1", confirmation.ShipmentId);
        Assert.Equal("TRK-1", confirmation.TrackingNumber);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://erp.example.com/api/shipments", handler.Request.RequestUri!.ToString());
        Assert.Equal($"order-{OrderId}", handler.Request.Headers.GetValues("Idempotency-Key").Single());
        Assert.Equal("allocation-7", handler.Request.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Contains("\"warehouseCode\":\"WH-001\"", handler.Body);
    }

    [Fact]
    public async Task CreateShipment_ThrowsWithTheStatusCode_WhenTheErpRejectsTheRequest()
    {
        var service = CreateService(new RecordingHandler(HttpStatusCode.Conflict, body: null));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => service.CreateShipmentAsync(OrderId, "WH-001"));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
    }

    private static ExternalShipmentService CreateService(HttpMessageHandler handler)
    {
        // The correlation context of the job, with the ID of the request that allocated the order
        var correlation = new CorrelationContext();
        correlation.SetCorrelationId("allocation-7");

        return new(new HttpClient(handler) { BaseAddress = new Uri("https://erp.example.com/") },
            correlation, NullLogger<ExternalShipmentService>.Instance);
    }

    private sealed class RecordingHandler(HttpStatusCode status, object? body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(status);
            if (body is not null)
            {
                response.Content = JsonContent.Create(body);
            }

            return response;
        }
    }
}
