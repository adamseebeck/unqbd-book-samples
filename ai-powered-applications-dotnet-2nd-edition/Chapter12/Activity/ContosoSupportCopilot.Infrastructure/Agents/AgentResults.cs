using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Infrastructure.Agents;

public static class AgentResults
{
    // A transient failure is worth a retry; every other failure needs a person.
    public static AgentResult Failure(string agentName, AgentFailureMode mode) => new()
    {
        Response = mode == AgentFailureMode.Transient
            ? $"{agentName} is temporarily unavailable. Retry the request."
            : $"{agentName} could not complete the request. A support engineer needs to take over.",
        RequiresHandoff = mode != AgentFailureMode.Transient,
        Artifacts = new Dictionary<string, object> { [AgentArtifactKeys.FailureMode] = mode }
    };
}
