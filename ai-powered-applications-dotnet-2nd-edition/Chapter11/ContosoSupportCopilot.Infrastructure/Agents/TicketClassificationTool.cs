using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// Wraps the validated intent extraction from Chapter 5.
public sealed class TicketClassificationTool : IAgentTool
{
    public const string ToolName = "classify_ticket";

    private readonly IIntentExtractor _intentExtractor;

    public TicketClassificationTool(IIntentExtractor intentExtractor)
    {
        _intentExtractor = intentExtractor;
    }

    public string Name => ToolName;
    public string Description => "Classifies the current support ticket: intent, urgency, product area, and a confidence score from 0 to 1.";
    public Type InputType => typeof(NoToolInput);
    public Type OutputType => typeof(SupportIntent);

    public async Task<AgentToolResult> ExecuteAsync(object input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (context.CurrentTicket is not { } ticket)
        {
            return AgentToolResult.Failure(AgentFailureMode.Unrecoverable, "classify_ticket needs a current ticket.");
        }

        try
        {
            var intent = await _intentExtractor.ExtractAsync($"{ticket.Subject}\n{ticket.Description}", cancellationToken);
            return AgentToolResult.Success(intent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return AgentToolResult.Failure(AgentFailureClassifier.Classify(ex), ex.Message);
        }
    }
}
