using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Data;

public class InMemorySupportContext : ISupportContext
{
    private readonly List<SupportTicket> _tickets = SampleData.CreateTickets();
    private readonly List<KnowledgeArticle> _articles = SampleData.CreateArticles();

    public Task<SupportTicket?> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
        => Task.FromResult(_tickets.FirstOrDefault(t => t.Id == ticketId));
    public Task<IReadOnlyList<TicketMessage>> GetTicketMessagesAsync(Guid ticketId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TicketMessage>>(_tickets.FirstOrDefault(t => t.Id == ticketId)?.Messages ?? []);
    public Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(string productArea, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Where(t => t.Status == "Open" && t.ProductArea == productArea).ToList());
    public Task<IReadOnlyList<KnowledgeArticle>> SearchKnowledgeBaseAsync(string query, string productArea, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KnowledgeArticle>>(_articles.Where(a => a.ProductArea == productArea && a.IsPublished).ToList());
    public Task SaveTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        _tickets.RemoveAll(t => t.Id == ticket.Id);
        _tickets.Add(ticket);
        return Task.CompletedTask;
    }
}
