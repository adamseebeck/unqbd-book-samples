namespace WarehouseApi.Contracts;

public record LowStockAlert(
    string Sku,
    string WarehouseCode,
    int OnHandQuantity,
    int ReorderThreshold
);
