using ContosoSupportCopilot.Domain.Workflows;

namespace ContosoSupportCopilot.Domain.Repositories;

public interface IWorkflowStateRepository
{
    Task<WorkflowState?> GetAsync(Guid workflowId, CancellationToken cancellationToken = default);

    // Returns the state with its new version. Throws WorkflowConflictException when another request
    // saved the workflow after this state was loaded.
    Task<WorkflowState> SaveAsync(WorkflowState state, CancellationToken cancellationToken = default);
}
