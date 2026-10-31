using System.Text.RegularExpressions;
using ContosoSupportCopilot.Domain.Helpers;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.AI;

public sealed partial class RagOrchestrator : IRagOrchestrator
{
    public const string NoContentResponse =
        "I cannot find any relevant knowledge base articles for this query. Please consult a senior engineer.";

    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorSearchService _vectorSearch;
    private readonly ISupportContext _supportContext;
    private readonly IConversationRepository _conversations;
    private readonly IAuthorizationFilter _authFilter;
    private readonly IContentSanitizer _sanitizer;
    private readonly ISensitiveDataMasker _dataMasker;
    private readonly RagPromptBuilder _promptBuilder;
    private readonly ISupportChatClient _chatClient;
    private readonly double _defaultMinScore;
    private readonly int _maxOutputTokens;
    private readonly ILogger<RagOrchestrator> _logger;

    public RagOrchestrator(
        IEmbeddingClient embeddingClient,
        IVectorSearchService vectorSearch,
        ISupportContext supportContext,
        IConversationRepository conversations,
        IAuthorizationFilter authFilter,
        IContentSanitizer sanitizer,
        ISensitiveDataMasker dataMasker,
        RagPromptBuilder promptBuilder,
        ISupportChatClient chatClient,
        IOptions<VectorStoreOptions> vectorOptions,
        IOptions<RagOptions> ragOptions,
        ILogger<RagOrchestrator> logger)
    {
        _embeddingClient = embeddingClient;
        _vectorSearch = vectorSearch;
        _supportContext = supportContext;
        _conversations = conversations;
        _authFilter = authFilter;
        _sanitizer = sanitizer;
        _dataMasker = dataMasker;
        _promptBuilder = promptBuilder;
        _chatClient = chatClient;
        _defaultMinScore = vectorOptions.Value.MinScore;
        _maxOutputTokens = ragOptions.Value.MaxOutputTokens;
        _logger = logger;
    }

    public async Task<RagResult> GenerateGroundedResponseAsync(
        RagRequest request,
        SupportTicket ticket,
        CancellationToken cancellationToken = default)
    {
        // The ticket decides the product area: retrieval searches that area, and the authorization filter
        // checks the same one. A request that names another ticket or area is a caller error, not an empty answer.
        if (request.TicketId != Guid.Empty && request.TicketId != ticket.Id)
        {
            throw new ArgumentException($"The request names ticket {request.TicketId}, not ticket {ticket.Id}.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.ProductArea)
            && !string.Equals(request.ProductArea, ticket.ProductArea, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"The request's product area '{request.ProductArea}' does not match the ticket's '{ticket.ProductArea}'.",
                nameof(request));
        }

        // The customer wrote the ticket, so it is screened too. A hit is not dropped: the ticket still needs an
        // answer, and the prompt escapes it as data, but a person reads the result before anyone uses it.
        // XmlSafe first, so no control character can hide a phrase or an address from the checks.
        var ticketText = PromptTextHelper.XmlSafe(DescribeTicket(ticket));
        var requiresReview = _sanitizer.ContainsMaliciousPatterns(ticketText);
        if (requiresReview)
        {
            _logger.LogWarning("Ticket {TicketId} contains possible prompt injection; its answer needs review", ticket.Id);
        }

        // 1. Retrieve twice the chunks needed, so the filters below still leave enough.
        var embedding = await _embeddingClient.GenerateAsync(request.Query, cancellationToken: cancellationToken);
        var searchRequest = new VectorSearchRequest
        {
            TopK = request.MaxResults * 2,
            MinScore = request.MinRelevanceScore ?? _defaultMinScore,
            ProductAreaFilter = string.IsNullOrWhiteSpace(ticket.ProductArea) ? null : ticket.ProductArea
        };
        var results = await _vectorSearch.SearchAsync(embedding.Vector, searchRequest, cancellationToken);

        // 2. The database, not the index, decides which articles exist and are published.
        var articleIds = results.Select(r => r.ArticleId).Distinct().ToList();
        var articles = (await _supportContext.GetArticlesAsync(articleIds, cancellationToken))
            .Where(a => a.IsPublished)
            .ToDictionary(a => a.Id);

        var sources = new List<CitationReference>();
        var seenChunks = new HashSet<(Guid ArticleId, int ChunkIndex)>();
        foreach (var result in results)
        {
            if (!seenChunks.Add((result.ArticleId, result.Chunk.ChunkIndex))
                || !articles.TryGetValue(result.ArticleId, out var article))
            {
                continue;
            }

            // 3. Authorization comes first: nothing else touches content the engineer may not read.
            if (!await _authFilter.CanAccessAsync(request.RequesterId, ticket, article, cancellationToken))
            {
                _logger.LogDebug("{RequesterId} may not read article {ArticleId}", request.RequesterId, article.Id);
                continue;
            }

            // 4. A chunk with injection patterns, in its text or its article's title, is excluded, not cleaned.
            var articleTitle = PromptTextHelper.XmlSafe(article.Title);
            var chunkText = PromptTextHelper.XmlSafe(result.Chunk.Content);
            if (_sanitizer.ContainsMaliciousPatterns(articleTitle) || _sanitizer.ContainsMaliciousPatterns(chunkText))
            {
                _logger.LogWarning(
                    "Excluded chunk {ChunkIndex} of article {ArticleId}: possible prompt injection",
                    result.Chunk.ChunkIndex,
                    article.Id);
                continue;
            }

            // 5. Mask sensitive data, then redact override phrases that slipped past detection.
            var title = _sanitizer.Sanitize(_dataMasker.MaskSensitiveContent(articleTitle));
            var content = _sanitizer.Sanitize(_dataMasker.MaskSensitiveContent(chunkText));
            sources.Add(new CitationReference
            {
                ArticleId = article.Id,
                ArticleTitle = title,
                ChunkIndex = result.Chunk.ChunkIndex,
                Content = content,
                Excerpt = content.Length > 200 ? content[..200] + "..." : content,
                RelevanceScore = result.Score
            });

            if (sources.Count >= request.MaxResults)
            {
                break;
            }
        }

        // An empty context invites the model to answer from memory, so the pipeline stops here.
        if (sources.Count == 0)
        {
            return new RagResult { Response = NoContentResponse, EmbeddingTokens = embedding.TokenCount, RequiresReview = requiresReview };
        }

        // 6. Generate: the instructions go in the system prompt, the articles and the ticket are data.
        var template = request.Prompt ?? SupportPrompts.GroundedAnswer;
        var conversation = await _conversations.GetByTicketAsync(ticket.Id, cancellationToken);
        var history = conversation?.Turns.OrderBy(t => t.TurnNumber).TakeLast(3).ToList() ?? [];
        var prompt = _promptBuilder.BuildGroundedPrompt(template, ticketText, sources, history);
        var options = new SupportChatOptions { SystemPrompt = template.Instructions, MaxOutputTokens = _maxOutputTokens };

        // The chat client runs the call through the Chapter 4 pipeline: its timeouts, retries and circuit breaker.
        var response = await _chatClient.CompleteAsync(prompt, options, cancellationToken);

        // 7. Validate: a grounded answer ended normally, cites at least one source, and invents none. An answer cut
        // off at Rag:MaxOutputTokens, or by a content filter, can hold a valid citation and still be incomplete.
        var stoppedEarly = response.FinishReason == ChatFinishReason.Length.Value || response.FinishReason == ChatFinishReason.ContentFilter.Value;
        if (stoppedEarly)
        {
            _logger.LogWarning("The answer for ticket {TicketId} stopped early ({FinishReason})", ticket.Id, response.FinishReason);
        }

        var cited = CitationMarker().Matches(response.Content)
            .Select(match => Guid.TryParse(match.Groups["id"].Value, out var id) ? id : Guid.Empty)
            .Distinct()
            .ToList();
        var provided = sources.Select(s => s.ArticleId).ToHashSet();

        return new RagResult
        {
            Response = response.Content,
            Citations = sources,
            CitedArticleIds = cited.Where(provided.Contains).ToList(),
            IsGrounded = !stoppedEarly && cited.Count > 0 && cited.All(provided.Contains),
            FinishReason = response.FinishReason,
            RequiresReview = requiresReview,
            InputTokens = response.InputTokens,
            OutputTokens = response.OutputTokens,
            EmbeddingTokens = embedding.TokenCount,
            ModelId = response.ModelId
        };
    }

    // The same labels as the drafting prompt from Chapter 3: Customer, or Support for an engineer's reply.
    private static string DescribeTicket(SupportTicket ticket)
    {
        var lines = new List<string> { $"Subject: {ticket.Subject}", $"Description: {ticket.Description}" };
        lines.AddRange(ticket.Messages
            .OrderBy(m => m.SentAt)
            .Select(m => $"{(m.IsFromCustomer ? "Customer" : "Support")}: {m.Content}"));
        return string.Join(Environment.NewLine, lines);
    }

    // A citation marker such as [Article: Handle API rate limits (9e8d7c6b-0003-4a5b-8c7d-000000000003)]. Only the ID
    // after the opening parenthesis counts: models sometimes drop the closing parenthesis or bracket, so the pattern
    // ends at the ID.
    [GeneratedRegex(@"[[]Article:[^]]*?[(](?<id>[0-9A-Fa-f-]{36})")]
    private static partial Regex CitationMarker();
}
