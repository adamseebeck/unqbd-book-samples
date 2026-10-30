using System.Runtime.Serialization;

namespace WarehouseApi.Contracts;

// The status monitor of an allocation: the ERP shipment of one order
[DataContract(Name = "Shipment", Namespace = "")]
public record ShipmentResponse(
    [property: DataMember(Order = 1)] Guid OrderId,
    [property: DataMember(Order = 2)] ShipmentStatus Status,
    [property: DataMember(Order = 3)] string? ShipmentId,
    [property: DataMember(Order = 4)] string? TrackingNumber,
    [property: DataMember(Order = 5)] int Attempts,
    [property: DataMember(Order = 6)] DateTime RequestedAt,
    [property: DataMember(Order = 7)] DateTime? RegisteredAt,
    [property: DataMember(Order = 8)] string? LastError
);
