namespace ContosoSupportCopilot.Domain.Workflows;

public interface ISupportWorkflow
{
    Task<WorkflowResult> ExecuteAsync(Guid ticketId, string engineerId, string initialInput, CancellationToken cancellationToken = default);

    // Continues a stored workflow from its current step for the engineer who started it; a finished one
    // returns its result again.
    Task<WorkflowResult> ResumeAsync(Guid workflowId, string engineerId, CancellationToken cancellationToken = default);
}
