namespace ContosoSupportCopilot.Domain.Workflows;

// Another request changed the same workflow first. This one stops instead of overwriting that work.
public class WorkflowConflictException : Exception
{
    public WorkflowConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
