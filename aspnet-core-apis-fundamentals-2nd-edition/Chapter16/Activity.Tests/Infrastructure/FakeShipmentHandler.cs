using System.Net;
using System.Net.Http.Json;

namespace WarehouseApi.Tests.Infrastructure;

// A hand-written test double for the ERP: the typed client's requests end here instead of on the network
public class FakeShipmentHandler : HttpMessageHandler
{
    private readonly Lock _lock = new();
    private readonly List<string> _idempotencyKeys = [];
    private readonly Dictionary<string, string> _shipmentIds = [];
    private int _failuresLeft;

    public IReadOnlyList<string> IdempotencyKeys
    {
        get
        {
            lock (_lock)
            {
                return _idempotencyKeys.ToList();
            }
        }
    }

    // The next N shipment requests answer 503 Service Unavailable
    public void FailNext(int requests)
    {
        lock (_lock)
        {
            _failuresLeft = requests;
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.Single() : "";

        lock (_lock)
        {
            _idempotencyKeys.Add(key);

            if (_failuresLeft > 0)
            {
                _failuresLeft--;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            // Like the ERP: the same key gets the same shipment
            if (!_shipmentIds.TryGetValue(key, out var shipmentId))
            {
                shipmentId = $"SHP-TEST-{_shipmentIds.Count + 1:D3}";
                _shipmentIds[key] = shipmentId;
            }

            var confirmation = new { shipmentId, confirmedAt = DateTimeOffset.UtcNow, trackingNumber = $"TRK-{shipmentId}" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) { Content = JsonContent.Create(confirmation) });
        }
    }
}
