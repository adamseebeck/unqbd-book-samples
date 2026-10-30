namespace ContosoSupportCopilot.Infrastructure.Data;

// One row per engineer and idempotency key; the pair is the primary key, so the database rejects a duplicate.
public class WorkflowRequestEntity
{
    public string RequesterId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid WorkflowId { get; set; }
    public DateTime CreatedAt { get; set; }
}
