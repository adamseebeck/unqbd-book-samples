using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface ISupportContext
{
    Task<SupportTicket?> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketMessage>> GetTicketMessagesAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(string productArea, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeArticle>> SearchKnowledgeBaseAsync(string query, string productArea, CancellationToken cancellationToken = default);
    Task SaveTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
}
