using System.Text.Json.Serialization;

namespace WarehouseApi.Contracts;

// Pending until the ERP confirms the shipment; Failed after the last attempt; Cancelled when the order was cancelled first
[JsonConverter(typeof(JsonStringEnumConverter<ShipmentStatus>))]
public enum ShipmentStatus
{
    Pending,
    Registered,
    Failed,
    Cancelled
}
