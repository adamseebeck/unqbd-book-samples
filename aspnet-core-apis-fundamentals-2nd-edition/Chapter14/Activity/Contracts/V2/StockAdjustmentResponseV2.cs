using System.Runtime.Serialization;

namespace WarehouseApi.Contracts.V2;

// V2 adds the audit fields that V1 clients never receive
[DataContract(Name = "StockAdjustment", Namespace = "")]
public record StockAdjustmentResponseV2(
    [property: DataMember(Order = 1)] Guid AdjustmentId,
    [property: DataMember(Order = 2)] string Sku,
    [property: DataMember(Order = 3)] string WarehouseCode,
    [property: DataMember(Order = 4)] int Quantity,
    [property: DataMember(Order = 5)] string ReasonCode,
    [property: DataMember(Order = 6)] int ReservedChange,
    [property: DataMember(Order = 7)] string? ReferenceNumber,
    [property: DataMember(Order = 8)] string? CorrelationId,
    [property: DataMember(Order = 9)] DateTime AdjustedAt
);
