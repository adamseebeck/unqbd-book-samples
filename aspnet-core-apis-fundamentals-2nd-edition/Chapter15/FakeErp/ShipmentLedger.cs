using Microsoft.Extensions.Options;

namespace FakeErp;

// The fake ERP's memory: the shipments it created, by Idempotency-Key, and the failures still to simulate
public class ShipmentLedger
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, RecordedShipment> _shipments = new();
    private readonly TimeSpan _delay;
    private int _requests;
    private int _failuresLeft;
    private int _lostResponsesLeft;

    public ShipmentLedger(IOptions<FakeErpOptions> options)
    {
        _delay = TimeSpan.FromMilliseconds(options.Value.DelayMilliseconds);
        _failuresLeft = options.Value.FailFirstRequests;
        _lostResponsesLeft = options.Value.LoseFirstResponses;
    }

    public bool IsUnavailable
    {
        get
        {
            lock (_lock)
            {
                return _failuresLeft > 0;
            }
        }
    }

    public async Task<(LedgerOutcome Outcome, ShipmentConfirmation? Confirmation)> RecordAsync(
        ShipmentRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        // Counted on arrival, so a request that is still waiting shows up in the summary
        lock (_lock)
        {
            _requests++;
        }

        if (_delay > TimeSpan.Zero)
        {
            await Task.Delay(_delay, cancellationToken);
        }

        lock (_lock)
        {
            if (_failuresLeft > 0)
            {
                _failuresLeft--;
                return (LedgerOutcome.Unavailable, null);
            }

            if (_shipments.TryGetValue(idempotencyKey, out var existing))
            {
                // A retry: the same key for the same order gets the shipment created the first time
                return existing.OrderId == request.OrderId
                    ? (LedgerOutcome.Replayed, existing.Confirmation)
                    : (LedgerOutcome.KeyReused, null);
            }

            var number = _shipments.Count + 1;
            var confirmation = new ShipmentConfirmation($"SHP-{number:D6}", DateTimeOffset.UtcNow, $"TRK{number:D9}");
            _shipments[idempotencyKey] = new RecordedShipment(idempotencyKey, request.OrderId, request.WarehouseCode, confirmation);

            if (_lostResponsesLeft > 0)
            {
                // Created, but the answer never reaches the client, as when a connection drops
                _lostResponsesLeft--;
                return (LedgerOutcome.Unavailable, null);
            }

            return (LedgerOutcome.Created, confirmation);
        }
    }

    public RecordedShipment? Find(string shipmentId)
    {
        lock (_lock)
        {
            return _shipments.Values.FirstOrDefault(s => s.Confirmation.ShipmentId == shipmentId);
        }
    }

    public void StartOutage(int requests)
    {
        lock (_lock)
        {
            _failuresLeft = Math.Max(0, requests);
        }
    }

    public LedgerSummary GetSummary()
    {
        lock (_lock)
        {
            return new LedgerSummary(_requests, _shipments.Values.ToList());
        }
    }
}
