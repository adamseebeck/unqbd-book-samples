namespace FakeErp;

// Requests counts every shipment request that arrived, failed ones and retries included
public record LedgerSummary(int Requests, IReadOnlyList<RecordedShipment> Shipments);
