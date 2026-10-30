using System.Text.Json;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace ContosoSupportCopilot.Infrastructure.Data;

public class SqlWorkflowStateRepository : IWorkflowStateRepository
{
    private readonly CopilotDbContext _dbContext;

    public SqlWorkflowStateRepository(CopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkflowState?> GetAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WorkflowStates
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<WorkflowState> SaveAsync(WorkflowState state, CancellationToken cancellationToken = default)
    {
        var saved = state with { Version = state.Version + 1 };
        var entity = MapToEntity(saved);
        var existing = await _dbContext.WorkflowStates.FindAsync([state.WorkflowId], cancellationToken);

        if (existing is null)
        {
            _dbContext.WorkflowStates.Add(entity);
        }
        else
        {
            // The UPDATE checks the version this state was loaded with, not the one this context last saw.
            _dbContext.Entry(existing).CurrentValues.SetValues(entity);
            _dbContext.Entry(existing).Property(e => e.Version).OriginalValue = state.Version;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return saved;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Another request saved this workflow first. Detach the rejected row, so a later read loads the stored one.
            _dbContext.Entry(existing!).State = EntityState.Detached;
            throw new WorkflowConflictException($"Another request saved workflow {state.WorkflowId} first.", ex);
        }
    }

    private static WorkflowState MapToDomain(WorkflowStateEntity entity) => new()
    {
        WorkflowId = entity.WorkflowId,
        TicketId = entity.TicketId,
        ConversationId = entity.ConversationId,
        CorrelationId = entity.CorrelationId,
        RequesterId = entity.RequesterId,
        InitialInput = entity.InitialInput,
        CurrentStep = Enum.Parse<WorkflowStep>(entity.CurrentStep),
        Artifacts = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entity.ArtifactsJson) ?? new(),
        StartedAt = entity.StartedAt,
        LastUpdatedAt = entity.LastUpdatedAt,
        RetryCount = entity.RetryCount,
        Version = entity.Version
    };

    private static WorkflowStateEntity MapToEntity(WorkflowState state) => new()
    {
        WorkflowId = state.WorkflowId,
        TicketId = state.TicketId,
        ConversationId = state.ConversationId,
        CorrelationId = state.CorrelationId,
        RequesterId = state.RequesterId,
        InitialInput = state.InitialInput,
        CurrentStep = state.CurrentStep.ToString(),
        ArtifactsJson = JsonSerializer.Serialize(state.Artifacts),
        StartedAt = state.StartedAt,
        LastUpdatedAt = state.LastUpdatedAt,
        RetryCount = state.RetryCount,
        Version = state.Version
    };
}
