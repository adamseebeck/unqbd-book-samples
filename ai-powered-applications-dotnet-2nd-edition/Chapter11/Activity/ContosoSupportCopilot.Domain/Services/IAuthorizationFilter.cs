using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IAuthorizationFilter
{
    Task<bool> CanAccessAsync(
        string requesterId,
        SupportTicket ticket,
        KnowledgeArticle article,
        CancellationToken cancellationToken = default);
}
