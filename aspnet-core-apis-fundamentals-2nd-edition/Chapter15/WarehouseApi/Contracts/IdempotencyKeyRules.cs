namespace WarehouseApi.Contracts;

// Part of the HTTP contract: the longest Idempotency-Key the API accepts. The endpoints check it before
// they call a service, and the idempotency table's key column has the same length
public static class IdempotencyKeyRules
{
    public const int MaxLength = 100;
}
