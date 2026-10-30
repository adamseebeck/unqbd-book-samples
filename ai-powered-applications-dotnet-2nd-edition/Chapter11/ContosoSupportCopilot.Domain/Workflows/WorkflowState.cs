using System.Text.Json;

namespace ContosoSupportCopilot.Domain.Workflows;

public record WorkflowState
{
    // Artifacts are JSON from the start, so a state loaded from the database reads like one in memory.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Guid WorkflowId { get; init; } = Guid.NewGuid();
    public Guid TicketId { get; init; }
    public Guid ConversationId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;

    // The engineer the workflow acts for; resuming keeps the same authorization.
    public string RequesterId { get; init; } = string.Empty;
    public string InitialInput { get; init; } = string.Empty;

    public WorkflowStep CurrentStep { get; init; } = WorkflowStep.Triage;
    public IReadOnlyDictionary<string, JsonElement> Artifacts { get; init; } = new Dictionary<string, JsonElement>();
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; init; } = DateTime.UtcNow;

    // Transient stops of the current step; a new step starts at zero.
    public int RetryCount { get; init; }

    // How many times the state was saved. A save from an older version fails, so a second runner of the
    // same workflow cannot overwrite the first one's progress.
    public int Version { get; init; }

    public WorkflowState WithStep(WorkflowStep step) =>
        this with { CurrentStep = step, RetryCount = 0, LastUpdatedAt = DateTime.UtcNow };

    public WorkflowState WithArtifact<T>(string key, T value)
    {
        var artifacts = new Dictionary<string, JsonElement>(Artifacts) { [key] = JsonSerializer.SerializeToElement(value, JsonOptions) };
        return this with { Artifacts = artifacts, LastUpdatedAt = DateTime.UtcNow };
    }

    public T? GetArtifact<T>(string key) =>
        Artifacts.TryGetValue(key, out var value) ? value.Deserialize<T>(JsonOptions) : default;

    public bool HasArtifact(string key) => Artifacts.ContainsKey(key);

    public WorkflowState WithoutArtifact(string key)
    {
        var artifacts = new Dictionary<string, JsonElement>(Artifacts);
        artifacts.Remove(key);
        return this with { Artifacts = artifacts, LastUpdatedAt = DateTime.UtcNow };
    }

    public WorkflowState IncrementRetry() =>
        this with { RetryCount = RetryCount + 1, LastUpdatedAt = DateTime.UtcNow };
}
