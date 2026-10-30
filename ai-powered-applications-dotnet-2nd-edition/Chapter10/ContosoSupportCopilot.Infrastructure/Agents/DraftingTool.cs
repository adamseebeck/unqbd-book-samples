using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// Wraps IDraftingService: Chapter 3's drafting call, in the Chapter 4 pipeline, with Chapter 6's fallback decorator.
public sealed class DraftingTool : IAgentTool
{
    public const string ToolName = "draft_reply";

    private readonly IDraftingService _draftingService;

    public DraftingTool(IDraftingService draftingService)
    {
        _draftingService = draftingService;
    }

    public string Name => ToolName;
    public string Description => "Drafts a reply to the customer for the current ticket.";
    public Type InputType => typeof(DraftingOptions);
    public Type OutputType => typeof(DraftReply);

    public async Task<AgentToolResult> ExecuteAsync(object input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (context.CurrentTicket is not { } ticket)
        {
            return AgentToolResult.Failure(AgentFailureMode.Unrecoverable, "draft_reply needs a current ticket.");
        }

        try
        {
            // The facts come from the context, which the application fills, never from the input.
            var options = (DraftingOptions)input with { Knowledge = context.Knowledge };
            var reply = await _draftingService.DraftReplyAsync(ticket, options, cancellationToken);
            return AgentToolResult.Success(reply);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return AgentToolResult.Failure(AgentFailureClassifier.Classify(ex), ex.Message);
        }
    }
}
