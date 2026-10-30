namespace ContosoSupportCopilot.Domain.Workflows;

// Checkpointed retry. The model calls inside each agent already retried through the Chapter 4 pipeline, so a
// step that still fails transiently is not run again at once: the run stops at that step with its state
// saved, and the caller resumes it after RetryAfter. After MaxRetries stops, the step's next transient failure
// goes to a person.
public sealed record WorkflowRetryPolicy(int MaxRetries, TimeSpan RetryAfter);
