using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Workflows;

// What each step keeps in the workflow state: typed, small, and safe to store.
public sealed record TriageOutcome(string Intent, string Urgency, string ProductArea, double Confidence, double? PriorityScore);

// Citations carry excerpts only; the chunk text stays in the knowledge base.
public sealed record KnowledgeOutcome(string Answer, bool IsGrounded, IReadOnlyList<CitationReference> Citations, string SearchQuery);

public sealed record DraftOutcome(string Text, bool IsFallback, string ModelId);

public sealed record EscalationOutcome(EscalationDraft Draft, double? PriorityScore);

public static class WorkflowArtifactKeys
{
    public const string Triage = "triage";
    public const string Knowledge = "knowledge";
    public const string Draft = "draft";
    public const string Escalation = "escalation";
    public const string HandoffFrom = "handoff_from";
    public const string Error = "error";

    // When a transient failure stopped the run: the earliest time to resume it.
    public const string RetryAfter = "retry_after";
}
