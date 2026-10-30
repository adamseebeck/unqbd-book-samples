using System.Runtime.Serialization;

namespace WarehouseApi.Contracts;

// Without a data contract, an XML request for a list of these records would fail with 500
[DataContract(Name = "StockAdjustment", Namespace = "")]
public record StockAdjustmentResponseV1(
    [property: DataMember(Order = 1)] Guid AdjustmentId,
    [property: DataMember(Order = 2)] string Sku,
    [property: DataMember(Order = 3)] string WarehouseCode,
    [property: DataMember(Order = 4)] int Quantity,
    [property: DataMember(Order = 5)] string ReasonCode
);
