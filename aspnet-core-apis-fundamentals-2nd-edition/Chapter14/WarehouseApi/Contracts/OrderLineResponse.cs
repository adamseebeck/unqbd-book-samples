using System.Runtime.Serialization;

namespace WarehouseApi.Contracts;

[DataContract(Name = "OrderLine", Namespace = "")]
public record OrderLineResponse(
    [property: DataMember(Order = 1)] string Sku,
    [property: DataMember(Order = 2)] int RequestedQuantity,
    [property: DataMember(Order = 3)] int ReservedQuantity,
    [property: DataMember(Order = 4)] int ShippedQuantity
);
