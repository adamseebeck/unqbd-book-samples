namespace WarehouseApi.Models;

public record StockAlert(string Sku, string WarehouseCode, int CurrentQuantity);
