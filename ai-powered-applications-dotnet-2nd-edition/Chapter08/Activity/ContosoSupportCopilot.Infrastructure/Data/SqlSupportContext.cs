using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ContosoSupportCopilot.Infrastructure.Data;

public class SqlSupportContext : ISupportContext
{
    private readonly SupportDbContext _dbContext;

    public SqlSupportContext(SupportDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SupportTicket?> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SupportTickets
            .Include(t => t.Messages)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
    }

    public async Task<IReadOnlyList<TicketMessage>> GetTicketMessagesAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.SupportTickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        return ticket?.Messages.OrderBy(m => m.SentAt).ToList() ?? new List<TicketMessage>();
    }

    public async Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(string productArea, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SupportTickets
            .Where(t => t.Status == "Open" && t.ProductArea == productArea)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeArticle>> SearchKnowledgeBaseAsync(string query, string productArea, CancellationToken cancellationToken = default)
    {
        // Filters by product area and publication status; VectorKnowledgeRepository adds vector search.
        return await _dbContext.KnowledgeArticles
            .Where(a => a.ProductArea == productArea && a.IsPublished)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeArticle>> GetArticlesAsync(IReadOnlyCollection<Guid> articleIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.KnowledgeArticles
            .AsNoTracking()
            .Where(a => articleIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task SaveTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        // A ticket changed with 'with' is a second instance with the same key, and EF Core tracks
        // only one. Copy its own fields onto the tracked entity instead of attaching the copy.
        var existing = await _dbContext.SupportTickets.FindAsync([ticket.Id], cancellationToken);
        if (existing is null)
        {
            _dbContext.SupportTickets.Add(ticket);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(ticket);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
