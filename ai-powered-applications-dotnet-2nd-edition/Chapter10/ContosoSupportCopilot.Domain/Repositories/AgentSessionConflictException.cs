namespace ContosoSupportCopilot.Domain.Repositories;

// Another run of the same agent, conversation and engineer saved the session first.
public class AgentSessionConflictException : Exception
{
    public AgentSessionConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
