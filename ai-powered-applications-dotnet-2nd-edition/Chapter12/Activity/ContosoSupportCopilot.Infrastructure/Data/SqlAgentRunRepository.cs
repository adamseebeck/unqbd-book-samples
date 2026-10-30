using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContosoSupportCopilot.Infrastructure.Data;

public class SqlAgentRunRepository : IAgentRunRepository
{
    private readonly CopilotDbContext _dbContext;

    public SqlAgentRunRepository(CopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(AgentRun run, CancellationToken cancellationToken = default)
    {
        _dbContext.AgentRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentRun>> GetByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AgentRuns
            .Where(r => r.ConversationId == conversationId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
