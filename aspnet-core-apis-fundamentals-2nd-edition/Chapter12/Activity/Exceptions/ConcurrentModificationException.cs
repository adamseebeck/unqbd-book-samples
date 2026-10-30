using WarehouseApi.Models;

namespace WarehouseApi.Exceptions;

// An optimistic concurrency conflict that no service handled itself
public class ConcurrentModificationException : ConflictException
{
    public ConcurrentModificationException(string resourceType, string resourceId, Exception? innerException = null)
        : base(
            $"{resourceType} '{resourceId}' was changed by another request. Read it again and retry.",
            "CONCURRENT_MODIFICATION",
            innerException)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    // The identifiers clients already use: an order's ID, or a stock item's warehouse code and SKU
    public string ResourceType { get; }
    public string ResourceId { get; }

    public override string ErrorType => "https://api.example.com/errors/concurrent-modification";
    public override string Title => "Concurrent Modification";

    // Describes the entity whose save failed with the identifiers clients already use; the stock item's
    // Guid key and the row versions stay inside the API
    public static ConcurrentModificationException For(object? entity, Exception? innerException = null) => entity switch
    {
        Order order => new("Order", order.Id.ToString(), innerException),
        StockItem item => new("StockItem", $"{item.WarehouseCode}/{item.ProductSku}", innerException),
        _ => new("Resource", "unknown", innerException)
    };
}
