namespace WarehouseApi.Models;

// The stored result of a stock adjustment or an order creation, keyed by the client's Idempotency-Key header
public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
