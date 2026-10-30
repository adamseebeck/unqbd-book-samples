using System.Runtime.Serialization;

namespace WarehouseApi.Contracts;

// The data contract lets the XML formatter write this positional record; System.Text.Json ignores it
[DataContract(Name = "Order", Namespace = "")]
public record OrderResponse(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string CustomerReference,
    [property: DataMember(Order = 3)] OrderStatus Status,
    [property: DataMember(Order = 4)] DateTime CreatedDate,
    [property: DataMember(Order = 5)] string? FulfillmentWarehouseCode,
    // The DataContract serializer writes IEnumerable<T> members, but not IReadOnlyList<T>
    [property: DataMember(Order = 6)] IEnumerable<OrderLineResponse> Lines
);
