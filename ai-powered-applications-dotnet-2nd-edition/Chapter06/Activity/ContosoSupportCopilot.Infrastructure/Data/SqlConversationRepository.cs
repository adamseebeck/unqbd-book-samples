using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContosoSupportCopilot.Infrastructure.Data;

public class SqlConversationRepository : IConversationRepository
{
    private readonly CopilotDbContext _dbContext;

    public SqlConversationRepository(CopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Owned turns load with their conversation, so these queries need no Include.
    public async Task<CopilotConversation?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CopilotConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
    }

    public async Task<CopilotConversation?> GetByTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CopilotConversations
            .FirstOrDefaultAsync(c => c.TicketId == ticketId && c.ConversationState == "Active", cancellationToken);
    }

    public async Task SaveAsync(CopilotConversation conversation, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.CopilotConversations
            .FirstOrDefaultAsync(c => c.Id == conversation.Id, cancellationToken);

        if (existing == null)
        {
            _dbContext.CopilotConversations.Add(conversation);
        }
        else
        {
            // SetValues copies scalar properties only, so the new turns are added explicitly.
            _dbContext.Entry(existing).CurrentValues.SetValues(conversation);
            var lastSavedTurn = existing.Turns.Count == 0 ? 0 : existing.Turns.Max(t => t.TurnNumber);
            existing.Turns.AddRange(conversation.Turns.Where(t => t.TurnNumber > lastSavedTurn).ToList());
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // CurrentTurn is a concurrency token: another request saved a turn after this one loaded the conversation.
            // Clearing the tracked entities makes the caller's next read load the conversation as it is now.
            _dbContext.ChangeTracker.Clear();
            throw new ConversationConflictException(conversation.TicketId, ex);
        }
        catch (DbUpdateException ex) when (existing == null)
        {
            // The unique index allows one active conversation per ticket; another request may have created it first.
            _dbContext.ChangeTracker.Clear();
            if (!await _dbContext.CopilotConversations.AnyAsync(
                    c => c.TicketId == conversation.TicketId && c.ConversationState == "Active", cancellationToken))
            {
                throw;
            }

            throw new ConversationConflictException(conversation.TicketId, ex);
        }
    }

    // The active conversation's turns, newest first, one page at a time. The database sorts and pages,
    // so a long conversation is never loaded whole. Owned turns can be queried without their owner only
    // without tracking.
    public async Task<IReadOnlyList<ConversationTurn>> GetConversationHistoryAsync(
        Guid ticketId,
        int pageSize,
        int offset,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        return await _dbContext.CopilotConversations
            .AsNoTracking()
            .Where(c => c.TicketId == ticketId && c.ConversationState == "Active")
            .SelectMany(c => c.Turns)
            .OrderByDescending(t => t.TurnNumber)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
