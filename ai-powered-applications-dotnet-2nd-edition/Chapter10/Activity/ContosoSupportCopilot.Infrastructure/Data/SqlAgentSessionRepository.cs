using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContosoSupportCopilot.Infrastructure.Data;

public class SqlAgentSessionRepository : IAgentSessionRepository
{
    private readonly CopilotDbContext _dbContext;

    public SqlAgentSessionRepository(CopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AgentSessionRecord?> GetAsync(Guid conversationId, string agentName, string requesterId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AgentSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ConversationId == conversationId && s.AgentName == agentName && s.RequesterId == requesterId, cancellationToken);
    }

    public async Task<int> SaveAsync(AgentSessionRecord session, CancellationToken cancellationToken = default)
    {
        var saved = session with { Version = session.Version + 1, UpdatedAt = DateTime.UtcNow };
        var existing = await _dbContext.AgentSessions.FindAsync([session.ConversationId, session.AgentName, session.RequesterId], cancellationToken);
        if (existing is null)
        {
            _dbContext.AgentSessions.Add(saved);
        }
        else
        {
            // The UPDATE checks the version this run loaded, not the one this context last saw.
            _dbContext.Entry(existing).CurrentValues.SetValues(saved);
            _dbContext.Entry(existing).Property(e => e.Version).OriginalValue = session.Version;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return saved.Version;
        }
        catch (DbUpdateException ex)
        {
            // Detach the rejected row, so a later read loads the stored one.
            foreach (var entry in ex.Entries)
            {
                entry.State = EntityState.Detached;
            }

            // A stale update, or a first insert that lost to another run's first insert, is a conflict;
            // any other failure of the insert is not.
            if (ex is DbUpdateConcurrencyException || (existing is null && await ExistsAsync(session, cancellationToken)))
            {
                throw new AgentSessionConflictException($"Another run saved the {session.AgentName} session first.", ex);
            }

            throw;
        }
    }

    private Task<bool> ExistsAsync(AgentSessionRecord session, CancellationToken cancellationToken) =>
        _dbContext.AgentSessions.AnyAsync(
            s => s.ConversationId == session.ConversationId && s.AgentName == session.AgentName && s.RequesterId == session.RequesterId,
            cancellationToken);
}
