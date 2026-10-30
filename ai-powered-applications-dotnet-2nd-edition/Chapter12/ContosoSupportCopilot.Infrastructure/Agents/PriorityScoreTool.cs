using System.Globalization;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// A Semantic Kernel plugin function from Chapter 4 behind the same tool contract.
public sealed class PriorityScoreTool : IAgentTool
{
    public const string ToolName = "calculate_priority_score";

    private readonly IKernelOrchestrator _orchestrator;

    public PriorityScoreTool(IKernelOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public string Name => ToolName;
    public string Description => "Calculates the current ticket's priority score from its priority, the customer's tier, and its age.";
    public Type InputType => typeof(NoToolInput);
    public Type OutputType => typeof(double);

    public async Task<AgentToolResult> ExecuteAsync(object input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (context.CurrentTicket is not { } ticket)
        {
            return AgentToolResult.Failure(AgentFailureMode.Unrecoverable, "calculate_priority_score needs a current ticket.");
        }

        try
        {
            var score = await _orchestrator.ExecutePluginFunctionAsync(
                "SupportTicketPlugin",
                "calculate_priority_score",
                new Dictionary<string, object> { ["ticketId"] = ticket.Id },
                cancellationToken);
            return AgentToolResult.Success(double.Parse(score, CultureInfo.InvariantCulture));
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return AgentToolResult.Failure(AgentFailureClassifier.Classify(ex), ex.Message);
        }
    }
}
