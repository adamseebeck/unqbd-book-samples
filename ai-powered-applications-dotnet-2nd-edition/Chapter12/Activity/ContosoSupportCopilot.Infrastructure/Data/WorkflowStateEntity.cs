namespace ContosoSupportCopilot.Infrastructure.Data;

// The table row: simple columns plus the artifacts as one JSON document, so a new artifact
// type needs no new column.
public class WorkflowStateEntity
{
    public Guid WorkflowId { get; set; }
    public Guid TicketId { get; set; }
    public Guid ConversationId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string RequesterId { get; set; } = string.Empty;
    public string InitialInput { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public string ArtifactsJson { get; set; } = "{}";
    public DateTime StartedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public int RetryCount { get; set; }

    // A concurrency token: an update applies only while the row still holds the version that was read.
    public int Version { get; set; }
}
