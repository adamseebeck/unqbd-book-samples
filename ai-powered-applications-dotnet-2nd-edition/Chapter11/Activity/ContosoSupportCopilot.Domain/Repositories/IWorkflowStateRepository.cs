using ContosoSupportCopilot.Domain.Workflows;

namespace ContosoSupportCopilot.Domain.Repositories;

public interface IWorkflowStateRepository
{
    Task<WorkflowState?> GetAsync(Guid workflowId, CancellationToken cancellationToken = default);

    // Returns the state with its new version. Throws WorkflowConflictException when another request
    // saved the workflow after this state was loaded.
    Task<WorkflowState> SaveAsync(WorkflowState state, CancellationToken cancellationToken = default);

    // The workflow that this engineer's idempotency key started, if any. Keys belong to the engineer who
    // sent them, so the same key from another engineer never returns this engineer's workflow.
    Task<Guid?> FindByIdempotencyKeyAsync(string requesterId, string idempotencyKey, CancellationToken cancellationToken = default);

    // False when the engineer's key already belongs to a workflow: the database keeps each pair once.
    Task<bool> TryRecordIdempotencyKeyAsync(string requesterId, string idempotencyKey, Guid workflowId, CancellationToken cancellationToken = default);
}
