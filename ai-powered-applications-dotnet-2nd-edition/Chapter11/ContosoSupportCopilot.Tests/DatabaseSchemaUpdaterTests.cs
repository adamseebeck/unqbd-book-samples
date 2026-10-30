using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class DatabaseSchemaUpdaterTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public DatabaseSchemaUpdaterTests() => _connection.Open();

    [Fact]
    public async Task CreateMissingTablesAsync_AddsANewTable_AndKeepsTheData()
    {
        // A database from an earlier chapter: every table except AgentSessions, with a conversation in it.
        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("DROP TABLE \"AgentSessions\"");
            context.CopilotConversations.Add(new CopilotConversation { Id = Guid.NewGuid(), TicketId = Guid.NewGuid() });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            Assert.False(await context.Database.EnsureCreatedAsync());
            Assert.Equal(["AgentSessions"], await context.CreateMissingTablesAsync());
            Assert.Empty(await context.CreateMissingTablesAsync());

            context.AgentSessions.Add(new AgentSessionRecord { ConversationId = Guid.NewGuid(), AgentName = "KnowledgeAgent", SessionJson = "{}" });
            await context.SaveChangesAsync();
            Assert.Equal(1, await context.CopilotConversations.CountAsync());
        }
    }

    [Fact]
    public void PlanMissingTables_ForSqlServer_UsesTheProvidersTypes()
    {
        // No connection is opened: the SQL comes from the model and the SQL Server provider.
        using var context = new CopilotDbContext(new DbContextOptionsBuilder<CopilotDbContext>()
            .UseSqlServer("Server=localhost;Database=ContosoCopilotState;Trusted_Connection=True").Options);

        var (tables, commands) = DatabaseSchemaUpdater.PlanMissingTables(context, ["CopilotConversations", "ConversationTurn", "AgentRuns", "WorkflowStates"]);

        Assert.Equal(["AgentSessions"], tables);
        var create = Assert.Single(commands);
        Assert.Contains("CREATE TABLE [AgentSessions]", create);
        Assert.Contains("[RequesterId] nvarchar(100) NOT NULL", create);
    }

    [Fact]
    public async Task CreateMissingTablesAsync_AddsWorkflowStates_ToAChapter10Database()
    {
        // The Chapter 10 schema: every table except WorkflowStates.
        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("DROP TABLE \"WorkflowStates\"");
        }

        await using (var context = CreateContext())
        {
            Assert.Equal(["WorkflowStates"], await context.CreateMissingTablesAsync());
            context.WorkflowStates.Add(new WorkflowStateEntity { WorkflowId = Guid.NewGuid(), CurrentStep = "Triage" });
            await context.SaveChangesAsync();
            Assert.Equal(1, await context.WorkflowStates.CountAsync());
        }
    }

    private CopilotDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CopilotDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
