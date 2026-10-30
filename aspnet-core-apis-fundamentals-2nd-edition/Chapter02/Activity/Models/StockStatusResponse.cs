namespace WarehouseApi.Models;

public record StockStatusResponse(string Sku, int Quantity, string Status, string? NotificationEmail);
