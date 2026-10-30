using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Data;

// Decorates the relational ISupportContext: tickets come from the database, knowledge from vector search.
public class VectorKnowledgeRepository : ISupportContext
{
    private readonly ISupportContext _innerContext;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IArticleChunkingService _chunkingService;
    private readonly VectorStoreOptions _options;
    private readonly ILogger<VectorKnowledgeRepository> _logger;

    public VectorKnowledgeRepository(
        ISupportContext innerContext,
        IEmbeddingClient embeddingClient,
        IVectorSearchService vectorSearch,
        IArticleChunkingService chunkingService,
        IOptions<VectorStoreOptions> options,
        ILogger<VectorKnowledgeRepository> logger)
    {
        _innerContext = innerContext;
        _embeddingClient = embeddingClient;
        _vectorSearch = vectorSearch;
        _chunkingService = chunkingService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<KnowledgeArticle>> SearchKnowledgeBaseAsync(
        string query,
        string productArea,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var embedding = await _embeddingClient.GenerateAsync(query, cancellationToken: cancellationToken);
        var searchRequest = new VectorSearchRequest
        {
            TopK = _options.TopK,
            MinScore = _options.MinScore,
            ProductAreaFilter = string.IsNullOrWhiteSpace(productArea) ? null : productArea
        };

        var results = await _vectorSearch.SearchAsync(embedding.Vector, searchRequest, cancellationToken);
        if (results.Count == 0)
        {
            return [];
        }

        // The index holds chunks; the database holds the current article. An article that was
        // deleted or unpublished after it was indexed is left out.
        var articleIds = results.Select(r => r.ArticleId).Distinct().ToList();
        var articles = (await _innerContext.GetArticlesAsync(articleIds, cancellationToken))
            .Where(a => a.IsPublished)
            .ToDictionary(a => a.Id);

        // Results arrive best first, so the articles stay in the order of their best chunk.
        return results
            .GroupBy(r => r.ArticleId)
            .Where(group => articles.ContainsKey(group.Key))
            .Select(group => articles[group.Key] with { Chunks = group.Select(r => r.Chunk).ToList() })
            .ToList();
    }

    public async Task<int> IndexArticleAsync(
        KnowledgeArticle article,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default)
    {
        var chunks = await _chunkingService.ChunkAsync(article, chunkingOptions, cancellationToken);

        var embedded = new List<ArticleChunk>(chunks.Count);
        var tokens = 0;
        foreach (var chunk in chunks)
        {
            // The title gives every chunk the article's topic, even when the chunk never names it.
            var embedding = await _embeddingClient.GenerateAsync($"{article.Title}\n{chunk.Content}", cancellationToken: cancellationToken);
            embedded.Add(chunk with { Embedding = embedding.Vector });
            tokens += embedding.TokenCount;
        }

        await _vectorSearch.UpsertArticleChunksAsync(article, embedded, cancellationToken);
        _logger.LogInformation(
            "Indexed article {ArticleId} as {ChunkCount} chunks ({Tokens} embedding tokens)",
            article.Id,
            embedded.Count,
            tokens);

        return embedded.Count;
    }

    // Everything else goes to the relational context.
    public Task<SupportTicket?> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
        => _innerContext.GetTicketAsync(ticketId, cancellationToken);

    public Task<IReadOnlyList<TicketMessage>> GetTicketMessagesAsync(Guid ticketId, CancellationToken cancellationToken = default)
        => _innerContext.GetTicketMessagesAsync(ticketId, cancellationToken);

    public Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(string productArea, CancellationToken cancellationToken = default)
        => _innerContext.GetOpenTicketsAsync(productArea, cancellationToken);

    public Task<IReadOnlyList<KnowledgeArticle>> GetArticlesAsync(IReadOnlyCollection<Guid> articleIds, CancellationToken cancellationToken = default)
        => _innerContext.GetArticlesAsync(articleIds, cancellationToken);

    public Task SaveTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
        => _innerContext.SaveTicketAsync(ticket, cancellationToken);
}
