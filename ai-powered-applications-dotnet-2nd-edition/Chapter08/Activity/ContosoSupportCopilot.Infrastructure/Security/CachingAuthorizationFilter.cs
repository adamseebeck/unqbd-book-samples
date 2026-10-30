using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Security;

// A request-scoped decorator: several chunks of one article need only one authorization decision.
public sealed class CachingAuthorizationFilter : IAuthorizationFilter
{
    private readonly IAuthorizationFilter _inner;
    private readonly Dictionary<(string RequesterId, string ProductArea, Guid ArticleId), bool> _decisions = new();

    public CachingAuthorizationFilter(IAuthorizationFilter inner)
    {
        _inner = inner;
    }

    public int CheckCount { get; private set; }

    public int CachedDecisionCount => _decisions.Count;

    public async Task<bool> CanAccessAsync(
        string requesterId,
        SupportTicket ticket,
        KnowledgeArticle article,
        CancellationToken cancellationToken = default)
    {
        CheckCount++;

        // The decision depends on the engineer, the ticket's product area and the article.
        var key = (requesterId, ticket.ProductArea, article.Id);
        if (!_decisions.TryGetValue(key, out var allowed))
        {
            allowed = await _inner.CanAccessAsync(requesterId, ticket, article, cancellationToken);
            _decisions[key] = allowed;
        }

        return allowed;
    }
}
