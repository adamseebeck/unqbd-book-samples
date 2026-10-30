using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Security;

public sealed class ProductAreaAuthorizationFilter : IAuthorizationFilter
{
    private readonly RagOptions _options;

    public ProductAreaAuthorizationFilter(IOptions<RagOptions> options)
    {
        _options = options.Value;
    }

    public Task<bool> CanAccessAsync(
        string requesterId,
        SupportTicket ticket,
        KnowledgeArticle article,
        CancellationToken cancellationToken = default)
    {
        // The article must belong to the ticket's product area,
        var matchesTicket = string.Equals(ticket.ProductArea, article.ProductArea, StringComparison.OrdinalIgnoreCase);

        // and the engineer must be assigned to that area. An unknown engineer reads nothing.
        var isAssigned = _options.ProductAreasByEngineer.TryGetValue(requesterId, out var areas)
            && areas.Contains(article.ProductArea, StringComparer.OrdinalIgnoreCase);

        // The sample knowledge base serves every customer; tenant-specific articles add a tenant check here.
        return Task.FromResult(matchesTicket && isAssigned);
    }
}
