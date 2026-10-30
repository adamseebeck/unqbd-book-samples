namespace ContosoSupportCopilot.Domain.Workflows;

// The engineer sent an idempotency key again with another ticket or input. The first run belongs to the first
// request, so this one is rejected; a new request needs a new key.
public class IdempotencyKeyReusedException : Exception
{
    public IdempotencyKeyReusedException(string idempotencyKey)
        : base($"The idempotency key '{idempotencyKey}' was already used with a different ticket or input.")
    {
    }
}
