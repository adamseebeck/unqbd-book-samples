namespace FakeErp;

public record ShipmentConfirmation(string ShipmentId, DateTimeOffset ConfirmedAt, string TrackingNumber);
