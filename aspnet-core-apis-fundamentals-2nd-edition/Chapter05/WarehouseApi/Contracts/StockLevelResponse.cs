namespace WarehouseApi.Contracts;

public record StockLevelResponse(
    string Sku,
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int Available
);
