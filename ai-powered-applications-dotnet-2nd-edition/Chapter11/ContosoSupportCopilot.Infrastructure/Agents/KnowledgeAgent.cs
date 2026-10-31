using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.Agents;

public class KnowledgeAgent : IAgent
{
    // Another request can save a turn to the same conversation first; the turn is then added again.
    private const int MaxSaveAttempts = 3;

    private const string Instructions = """
        You find answers in the Contoso knowledge base for support engineers. Call
        search_knowledge_base with a short search query made from the ticket. Report only what the
        tool returns, and say so when it finds nothing.
        """;

    private readonly SupportAgentFactory _agentFactory;
    private readonly AgentToolRegistry _tools;
    private readonly AgentRunner _runner;
    private readonly IConversationRepository _conversationRepository;
    private readonly ICorrelationContext _correlationContext;

    public KnowledgeAgent(
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

    public string Name => AgentNames.Knowledge;

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var ticket = context.CurrentTicket ?? throw new InvalidOperationException("KnowledgeAgent requires a current ticket.");
        var conversation = await _conversationRepository.GetAsync(context.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {context.ConversationId} not found.");

        var toolContext = new AgentToolContext(_correlationContext.CorrelationId, context.ConversationId, conversation, ticket, context.RequesterId);
        var search = new AgentToolFunction(_tools.Resolve(KnowledgeRetrievalTool.ToolName), toolContext);
        var agent = _agentFactory.GetAgent(Name, Instructions);

        var outcome = await _runner.RunAsync(agent, context, $"{ticket.Subject}\n{ticket.Description}", [search],
            ChatToolMode.RequireSpecific(KnowledgeRetrievalTool.ToolName), cancellationToken);

        // The answer must come from the tool: text that the model wrote without searching is not grounded.
        var lookup = search.Calls.LastOrDefault(c => c.Result.IsSuccess) ?? search.Calls.LastOrDefault();
        if (outcome.Response is null || lookup?.Result.Value is not RagResult rag)
        {
            await _runner.LogRunAsync(context, Name, outcome, cancellationToken: cancellationToken);
            return AgentResults.Failure(Name, outcome.FailureMode ?? lookup?.Result.FailureMode ?? AgentFailureMode.Unrecoverable);
        }

        await _runner.LogRunAsync(context, Name, outcome, toolInputTokens: rag.InputTokens, toolOutputTokens: rag.OutputTokens,
            cancellationToken: cancellationToken);

        var query = ((KnowledgeSearchInput)lookup.Input!).Query;
        await SaveTurnAsync(context.ConversationId, query, rag.Response, cancellationToken);

        // Only the cited sources travel on, with their excerpts; the chunk text stays behind.
        var citations = rag.Citations
            .Where(c => rag.CitedArticleIds.Contains(c.ArticleId))
            .Select(c => c with { Content = string.Empty })
            .ToList();

        // A person takes over an answer that fails Chapter 8's citation check, or that answers a ticket which
        // carries instructions for the model.
        var needsPerson = !rag.IsGrounded || rag.RequiresReview;
        return new AgentResult
        {
            Response = rag.Response,
            RequiresHandoff = needsPerson,
            NextAgent = needsPerson ? AgentNames.Escalation : null,
            Artifacts = new Dictionary<string, object>
            {
                [AgentArtifactKeys.SearchQuery] = query,
                [AgentArtifactKeys.Citations] = citations,
                [AgentArtifactKeys.IsGrounded] = rag.IsGrounded,
                [AgentArtifactKeys.ModelId] = rag.ModelId,
                [AgentArtifactKeys.InputTokens] = rag.InputTokens,
                [AgentArtifactKeys.OutputTokens] = rag.OutputTokens,
                [AgentArtifactKeys.EmbeddingTokens] = rag.EmbeddingTokens
            }
        };
    }

    // As in Chapter 6: the save fails when another request saved a turn after this run loaded the
    // conversation, so the turn is added again to the conversation as it is now, without a second search.
    private async Task SaveTurnAsync(Guid conversationId, string query, string answer, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var conversation = await _conversationRepository.GetAsync(conversationId, cancellationToken)
                ?? throw new InvalidOperationException($"Conversation {conversationId} not found.");
            var turn = new ConversationTurn
            {
                TurnNumber = conversation.CurrentTurn + 1,
                UserInput = query,
                CopilotResponse = answer,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                await _conversationRepository.SaveAsync(
                    conversation with { CurrentTurn = turn.TurnNumber, UpdatedAt = DateTime.UtcNow, Turns = [.. conversation.Turns, turn] },
                    cancellationToken);
                return;
            }
            catch (ConversationConflictException) when (attempt < MaxSaveAttempts)
            {
                // The next pass loads the other request's turn and adds this one after it.
            }
        }
    }
}
