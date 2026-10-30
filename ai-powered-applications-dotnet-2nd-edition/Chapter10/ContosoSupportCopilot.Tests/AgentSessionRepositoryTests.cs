using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// The session store's concurrency check against a real SQLite database: two runs that save the same
// session, in either order, never overwrite each other.
public sealed class AgentSessionRepositoryTests : IDisposable
{
    private readonly string _connection = $"Data Source={Path.Combine(Path.GetTempPath(), $"contoso-sessions-{Guid.NewGuid():N}.db")}";
    private readonly AgentSessionRecord _session = new()
    {
        ConversationId = Guid.NewGuid(), AgentName = "KnowledgeAgent", RequesterId = "engineer-ana", SessionJson = "{}"
    };

    [Fact]
    public async Task SaveAsync_ReportsAConflict_WhenAnotherRunSavedFirst_AndAcceptsTheCurrentVersion()
    {
        await using (var setup = CreateContext())
        {
            await setup.Database.EnsureCreatedAsync();
        }

        // The other run's first save lands between this run's lookup and its insert.
        var otherRunSavesFirst = new BeforeFirstSave(async () =>
        {
            await using var other = CreateContext();
            await new SqlAgentSessionRepository(other).SaveAsync(_session);
        });
        await using var context = CreateContext(otherRunSavesFirst);
        var repository = new SqlAgentSessionRepository(context);

        // Two first saves: the insert that loses is a conflict, not a primary-key error.
        await Assert.ThrowsAsync<AgentSessionConflictException>(() => repository.SaveAsync(_session));

        // A save that started from version 0 cannot replace version 1; one from version 1 can.
        await Assert.ThrowsAsync<AgentSessionConflictException>(() => repository.SaveAsync(_session));
        Assert.Equal(2, await repository.SaveAsync(_session with { Version = 1 }));
        Assert.Equal(2, (await repository.GetAsync(_session.ConversationId, _session.AgentName, _session.RequesterId))!.Version);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(new SqliteConnectionStringBuilder(_connection).DataSource);
    }

    private CopilotDbContext CreateContext(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<CopilotDbContext>().UseSqlite(_connection).AddInterceptors(interceptors).Options);

    // Runs an action once, just before the context writes its changes.
    private sealed class BeforeFirstSave(Func<Task> action) : SaveChangesInterceptor
    {
        private bool _done;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_done)
            {
                _done = true;
                await action();
            }

            return result;
        }
    }
}
