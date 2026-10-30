namespace WarehouseApi.Contracts;

public record StockListItem(
    string Sku,
    string ProductName,
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int Available,
    int ReorderThreshold);
