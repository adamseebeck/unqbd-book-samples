namespace ContosoSupportCopilot.Domain.Workflows;

public interface ISupportWorkflow
{
    // With an idempotency key, a repeated request from the same engineer returns the first run's result
    // instead of starting another.
    Task<WorkflowResult> ExecuteAsync(Guid ticketId, string engineerId, string initialInput,
        string? idempotencyKey = null, CancellationToken cancellationToken = default);

    // Continues a stored workflow from its current step for the engineer who started it; a finished one
    // returns its result again.
    Task<WorkflowResult> ResumeAsync(Guid workflowId, string engineerId, CancellationToken cancellationToken = default);
}
