using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Infrastructure.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace ContosoSupportCopilot.Infrastructure.Data;

public class CopilotDbContext : DbContext
{
    public CopilotDbContext(DbContextOptions<CopilotDbContext> options) : base(options)
    {
    }

    public DbSet<CopilotConversation> CopilotConversations { get; set; } = null!;
    public DbSet<AgentRun> AgentRuns { get; set; } = null!;

    // From Chapter 10: each agent's serialized session per conversation.
    public DbSet<AgentSessionRecord> AgentSessions { get; set; } = null!;

    // From Chapter 11: the state of every multi-agent workflow run.
    public DbSet<WorkflowStateEntity> WorkflowStates { get; set; } = null!;

    // From Chapter 12: the token usage of every model call, for the cost reports.
    public DbSet<ModelUsageRecord> ModelUsage { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CopilotConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.OwnsMany(e => e.Turns, turn =>
            {
                turn.WithOwner().HasForeignKey("CopilotConversationId");
                turn.Property<int>("Id");
                turn.HasKey("Id");
            });
            entity.Property(e => e.ConversationState).HasMaxLength(50);

            // A save that started from an older turn count fails instead of overwriting a newer turn.
            entity.Property(e => e.CurrentTurn).IsConcurrencyToken();

            // One active conversation per ticket, even when two first requests arrive together.
            entity.HasIndex(e => e.TicketId).IsUnique().HasFilter("ConversationState = 'Active'");
        });

        modelBuilder.Entity<AgentRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.Property(e => e.AgentName).HasMaxLength(100);
            entity.Property(e => e.ModelId).HasMaxLength(100);
        });

        modelBuilder.Entity<AgentSessionRecord>(entity =>
        {
            entity.HasKey(e => new { e.ConversationId, e.AgentName, e.RequesterId });
            entity.Property(e => e.AgentName).HasMaxLength(100);
            entity.Property(e => e.RequesterId).HasMaxLength(100);
            entity.Property(e => e.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<WorkflowStateEntity>(entity =>
        {
            entity.HasKey(e => e.WorkflowId);
            entity.HasIndex(e => e.TicketId);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.Property(e => e.RequesterId).HasMaxLength(100);
            entity.Property(e => e.CurrentStep).HasMaxLength(50);
            entity.Property(e => e.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<ModelUsageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Operation).HasMaxLength(20);
            entity.Property(e => e.ModelId).HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
        });
    }
}
