using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Repositories;

public interface IConversationRepository
{
    Task<CopilotConversation?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<CopilotConversation?> GetByTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task SaveAsync(CopilotConversation conversation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationTurn>> GetConversationHistoryAsync(Guid ticketId, int pageSize, int offset, CancellationToken cancellationToken = default);
}
