using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.Agents;

public class TriageAgent : IAgent
{
    // Below this confidence, a person checks the classification before anything else runs.
    public const double MinimumConfidence = 0.7;

    private const string Instructions = """
        You triage Contoso support tickets for support engineers. Call classify_ticket and
        calculate_priority_score, then state the intent, urgency, product area, and priority score
        in one sentence. Do not write to the customer.
        """;

    private readonly SupportAgentFactory _agentFactory;
    private readonly AgentToolRegistry _tools;
    private readonly AgentRunner _runner;
    private readonly IConversationRepository _conversationRepository;
    private readonly ICorrelationContext _correlationContext;

    public TriageAgent(
        SupportAgentFactory agentFactory,
        AgentToolRegistry tools,
        AgentRunner runner,
        IConversationRepository conversationRepository,
        ICorrelationContext correlationContext)
    {
        _agentFactory = agentFactory;
        _tools = tools;
        _runner = runner;
        _conversationRepository = conversationRepository;
        _correlationContext = correlationContext;
    }

    public string Name => AgentNames.Triage;

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var ticket = context.CurrentTicket ?? throw new InvalidOperationException("TriageAgent requires a current ticket.");
        var conversation = await _conversationRepository.GetAsync(context.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {context.ConversationId} not found.");

        // The tools see this ticket and conversation; the model only decides when to call them.
        var toolContext = new AgentToolContext(_correlationContext.CorrelationId, context.ConversationId, conversation, ticket, context.RequesterId);
        var classify = new AgentToolFunction(_tools.Resolve(TicketClassificationTool.ToolName), toolContext);
        var priority = new AgentToolFunction(_tools.Resolve(PriorityScoreTool.ToolName), toolContext);
        var agent = _agentFactory.GetAgent(Name, Instructions);

        var outcome = await _runner.RunAsync(agent, context, $"{ticket.Subject}\n{ticket.Description}", [classify, priority],
            ChatToolMode.RequireSpecific(TicketClassificationTool.ToolName), cancellationToken);
        var classification = classify.Calls.LastOrDefault()?.Result;
        await _runner.LogRunAsync(context, Name, outcome, cancellationToken: cancellationToken);

        // The classification must come from the tool, never from the model's own text.
        if (outcome.Response is null || classification?.Value is not SupportIntent intent)
        {
            return AgentResults.Failure(Name, outcome.FailureMode ?? classification?.FailureMode ?? AgentFailureMode.Unrecoverable);
        }

        var artifacts = new Dictionary<string, object>
        {
            [AgentArtifactKeys.Intent] = intent.Intent,
            [AgentArtifactKeys.Urgency] = intent.Urgency,
            [AgentArtifactKeys.ProductArea] = intent.ProductArea,
            [AgentArtifactKeys.Confidence] = intent.ConfidenceScore
        };
        if (priority.Calls.LastOrDefault()?.Result.Value is double score)
        {
            artifacts[AgentArtifactKeys.PriorityScore] = score;
        }

        // The ticket is customer text that can steer the model's sentence, so that sentence is for display only;
        // every decision here uses the tool's typed result.
        var needsReview = intent.ConfidenceScore < MinimumConfidence;
        return new AgentResult
        {
            Response = outcome.Response.Text,
            RequiresHandoff = needsReview,
            NextAgent = needsReview ? AgentNames.Escalation : null,
            Artifacts = artifacts
        };
    }
}
