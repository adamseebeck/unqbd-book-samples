using System.Text.Json.Serialization;

namespace WarehouseApi.Contracts;

// Clients send and receive the status by name, for example "Allocated"
[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
public enum OrderStatus
{
    Pending,
    Allocated,
    Fulfilled,
    Cancelled
}
