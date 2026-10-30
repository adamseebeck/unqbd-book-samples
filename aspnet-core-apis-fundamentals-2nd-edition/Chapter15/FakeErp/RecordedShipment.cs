namespace FakeErp;

public record RecordedShipment(
    string IdempotencyKey,
    Guid OrderId,
    string WarehouseCode,
    ShipmentConfirmation Confirmation);
