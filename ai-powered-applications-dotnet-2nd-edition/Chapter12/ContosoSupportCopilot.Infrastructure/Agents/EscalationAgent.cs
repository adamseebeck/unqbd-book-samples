using System.Diagnostics;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// Prepares an engineering escalation with the validated EscalationDraftService from Chapter 5.
public class EscalationAgent : IAgent
{
    private readonly EscalationDraftService _escalationDraftService;
    private readonly IConversationRepository _conversationRepository;
    private readonly AgentToolRegistry _tools;
    private readonly AgentRunner _runner;
    private readonly ICorrelationContext _correlationContext;

    public EscalationAgent(
        EscalationDraftService escalationDraftService,
        IConversationRepository conversationRepository,
        AgentToolRegistry tools,
        AgentRunner runner,
        ICorrelationContext correlationContext)
    {
        _escalationDraftService = escalationDraftService;
        _conversationRepository = conversationRepository;
        _tools = tools;
        _runner = runner;
        _correlationContext = correlationContext;
    }

    public string Name => AgentNames.Escalation;

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetAsync(context.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {context.ConversationId} not found.");
        var ticket = context.CurrentTicket ?? throw new InvalidOperationException("EscalationAgent requires a current ticket.");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var draft = await _escalationDraftService.GenerateDraftAsync(ticket.Id, cancellationToken);

            // The same priority tool the triage agent uses, called directly.
            var toolContext = new AgentToolContext(_correlationContext.CorrelationId, context.ConversationId, conversation, ticket, context.RequesterId);
            var priority = await _tools.Resolve(PriorityScoreTool.ToolName).ExecuteAsync(new NoToolInput(), toolContext, cancellationToken);

            await _runner.LogRunAsync(context, Name, new AgentRunOutcome { LatencyMs = (int)stopwatch.ElapsedMilliseconds },
                cancellationToken: cancellationToken);

            var artifacts = new Dictionary<string, object>
            {
                [AgentArtifactKeys.EscalationDraft] = draft,
                [AgentArtifactKeys.GeneratedAt] = DateTime.UtcNow
            };
            if (priority.Value is double score)
            {
                artifacts[AgentArtifactKeys.PriorityScore] = score;
            }

            return new AgentResult { Response = draft.Summary, Artifacts = artifacts };
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // ArgumentException (unknown ticket), SchemaValidationException and other InvalidOperationExceptions are
            // Unrecoverable; TimeoutException and an open circuit are Transient; UnauthorizedAccessException needs a person.
            var mode = AgentFailureClassifier.Classify(ex);
            await _runner.LogRunAsync(context, Name,
                new AgentRunOutcome { FailureMode = mode, ErrorMessage = ex.Message, LatencyMs = (int)stopwatch.ElapsedMilliseconds },
                cancellationToken: cancellationToken);
            return AgentResults.Failure(Name, mode);
        }
    }
}
