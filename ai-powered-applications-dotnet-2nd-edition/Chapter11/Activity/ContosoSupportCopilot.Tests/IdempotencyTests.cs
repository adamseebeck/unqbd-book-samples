using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class IdempotencyTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public IdempotencyTests() => _connection.Open();

    [Fact]
    public async Task ExecuteAsync_WithTheSameKey_ReturnsTheFirstRun_AndRunsNoAgentTwice()
    {
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped<EscalationDraftService>());

        var first = await ProcessAsync(host, "retry-1");
        var runs = await CountRunsAsync(host);
        var repeated = await ProcessAsync(host, "retry-1");

        Assert.Equal(first.WorkflowId, repeated.WorkflowId);
        Assert.Equal(first.FinalResponse, repeated.FinalResponse);
        Assert.Equal(runs, await CountRunsAsync(host));
    }

    [Fact]
    public async Task ExecuteAsync_TheSameKeyFromAnotherEngineer_StartsThatEngineersOwnWorkflow()
    {
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped<EscalationDraftService>());

        var ana = await ProcessAsync(host, "shared-key");
        var ben = await ProcessAsync(host, "shared-key", "engineer-ben");

        // engineer-ben gets a run of his own, which escalates because he may not read Authentication articles.
        Assert.NotEqual(ana.WorkflowId, ben.WorkflowId);
        Assert.True(ben.EscalationRequired);
        Assert.Empty(ben.Citations);
    }

    [Fact]
    public async Task ExecuteAsync_TheSameKeyWithAnotherTicketOrInput_IsRejected_AndRunsNoAgent()
    {
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped<EscalationDraftService>());

        var first = await ProcessAsync(host, "retry-4");
        var runs = await CountRunsAsync(host);

        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(() => ProcessAsync(host, "retry-4", ticketId: SampleData.ApiTicketId));
        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(() => ProcessAsync(host, "retry-4", initialInput: "refund the charge"));

        // Nothing ran for either request, and the first request still gets its own run back
        Assert.Equal(runs, await CountRunsAsync(host));
        using (var scope = host.Services.CreateScope())
        {
            Assert.Null(await scope.ServiceProvider.GetRequiredService<IConversationRepository>().GetByTicketAsync(SampleData.ApiTicketId));
        }
        Assert.Equal(first.WorkflowId, (await ProcessAsync(host, "retry-4")).WorkflowId);
    }

    [Fact]
    public async Task TryRecordIdempotencyKeyAsync_KeepsEachKeyOncePerEngineer()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var repository = new SqlWorkflowStateRepository(context);
        var workflowId = Guid.NewGuid();

        Assert.True(await repository.TryRecordIdempotencyKeyAsync("engineer-ana", "retry-2", workflowId));
        Assert.False(await repository.TryRecordIdempotencyKeyAsync("engineer-ana", "retry-2", Guid.NewGuid()));
        Assert.True(await repository.TryRecordIdempotencyKeyAsync("engineer-ben", "retry-2", Guid.NewGuid()));
        Assert.Equal(workflowId, await repository.FindByIdempotencyKeyAsync("engineer-ana", "retry-2"));
    }

    [Fact]
    public async Task TryRecordIdempotencyKeyAsync_RethrowsAFailureThatIsNotADuplicate()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_requests BEFORE INSERT ON \"WorkflowRequests\" BEGIN SELECT RAISE(ABORT, 'disk full'); END;");

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new SqlWorkflowStateRepository(context).TryRecordIdempotencyKeyAsync("engineer-ana", "retry-3", Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateMissingTablesAsync_AddsWorkflowRequests_ToAChapter11Database()
    {
        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("DROP TABLE \"WorkflowRequests\"");
        }

        await using (var context = CreateContext())
        {
            Assert.Equal(["WorkflowRequests"], await context.CreateMissingTablesAsync());
        }
    }

    private static async Task<WorkflowResult> ProcessAsync(AgentTestHost host, string key, string engineerId = "engineer-ana",
        Guid? ticketId = null, string initialInput = "locked out")
    {
        using var scope = host.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
            .ExecuteAsync(ticketId ?? SampleData.PasswordTicketId, engineerId, initialInput, key);
    }

    private static async Task<int> CountRunsAsync(AgentTestHost host)
    {
        using var scope = host.Services.CreateScope();
        var conversation = await scope.ServiceProvider.GetRequiredService<IConversationRepository>().GetByTicketAsync(SampleData.PasswordTicketId);
        return (await scope.ServiceProvider.GetRequiredService<IAgentRunRepository>().GetByConversationAsync(conversation!.Id)).Count;
    }

    private CopilotDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CopilotDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
