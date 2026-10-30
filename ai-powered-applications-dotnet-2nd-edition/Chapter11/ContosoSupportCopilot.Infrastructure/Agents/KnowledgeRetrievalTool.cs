using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// What the agent's model reads: the grounded answer and its sources, not every retrieved chunk.
public sealed record KnowledgeToolOutput(string Answer, bool IsGrounded, IReadOnlyList<string> Sources);

// Wraps the RAG pipeline from Chapter 8. The model chooses the search query; the ticket, its
// product area and the engineer it searches for come from the context, never from the model.
public sealed class KnowledgeRetrievalTool : IAgentTool
{
    public const string ToolName = "search_knowledge_base";

    private readonly IRagOrchestrator _ragOrchestrator;

    public KnowledgeRetrievalTool(IRagOrchestrator ragOrchestrator)
    {
        _ragOrchestrator = ragOrchestrator;
    }

    public string Name => ToolName;
    public string Description => "Searches the Contoso knowledge base for the current ticket and returns a grounded answer with its sources.";
    public Type InputType => typeof(KnowledgeSearchInput);
    public Type OutputType => typeof(RagResult);

    public async Task<AgentToolResult> ExecuteAsync(object input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (context.CurrentTicket is not { } ticket)
        {
            return AgentToolResult.Failure(AgentFailureMode.Unrecoverable, "search_knowledge_base needs a current ticket.");
        }

        if (string.IsNullOrWhiteSpace(context.RequesterId))
        {
            return AgentToolResult.Failure(AgentFailureMode.Authorization, "search_knowledge_base needs the engineer it searches for.");
        }

        try
        {
            var search = (KnowledgeSearchInput)input;
            var request = new RagRequest
            {
                TicketId = ticket.Id,
                Query = search.Query,
                ProductArea = ticket.ProductArea,
                RequesterId = context.RequesterId
            };

            var result = await _ragOrchestrator.GenerateGroundedResponseAsync(request, ticket, cancellationToken);
            var sources = result.Citations
                .Where(c => result.CitedArticleIds.Contains(c.ArticleId))
                .Select(c => c.ArticleTitle)
                .Distinct()
                .ToList();

            return AgentToolResult.Success(result, new KnowledgeToolOutput(result.Response, result.IsGrounded, sources));
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return AgentToolResult.Failure(AgentFailureClassifier.Classify(ex), ex.Message);
        }
    }
}
