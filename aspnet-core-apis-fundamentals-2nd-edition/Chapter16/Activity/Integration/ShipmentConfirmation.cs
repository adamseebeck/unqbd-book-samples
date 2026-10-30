namespace WarehouseApi.Integration;

// What the ERP answers when it creates a shipment
public record ShipmentConfirmation(
    string ShipmentId,
    DateTimeOffset ConfirmedAt,
    string TrackingNumber);
