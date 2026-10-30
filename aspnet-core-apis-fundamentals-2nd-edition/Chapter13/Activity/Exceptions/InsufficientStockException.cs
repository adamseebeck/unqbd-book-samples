namespace WarehouseApi.Exceptions;

public class InsufficientStockException : ConflictException
{
    public InsufficientStockException(string sku, string warehouseCode, int requested, int available)
        : base(
            $"Insufficient stock of {sku} in {warehouseCode}: {available} available, {requested} requested.",
            "INSUFFICIENT_STOCK")
    {
        Sku = sku;
        WarehouseCode = warehouseCode;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }

    public string Sku { get; }
    public string WarehouseCode { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public override string ErrorType => "https://api.example.com/errors/insufficient-stock";
    public override string Title => "Insufficient Stock";
}
