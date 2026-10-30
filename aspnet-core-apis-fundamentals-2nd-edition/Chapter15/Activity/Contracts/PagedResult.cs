using System.Runtime.Serialization;

namespace WarehouseApi.Contracts;

// The data contract serves XML clients of the orders controller; Minimal API endpoints always write JSON,
// and XML carries only the data members, not the calculated properties
[DataContract(Name = "PagedResultOf{0}", Namespace = "")]
public record PagedResult<T>(
    [property: DataMember(Order = 1)] IEnumerable<T> Items,
    [property: DataMember(Order = 2)] int TotalCount,
    [property: DataMember(Order = 3)] int PageNumber,
    [property: DataMember(Order = 4)] int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
