using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Application.Workflows;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class SupportTicketWorkflowTests
{
    private static readonly SupportTicket Ticket = new() { Id = Guid.NewGuid(), Subject = "Cannot sign in", ProductArea = "Authentication" };
    private static readonly CitationReference Citation = new() { ArticleId = Guid.NewGuid(), ArticleTitle = "Restore access", Excerpt = "Clear the lockout." };

    private readonly Mock<IAgentCoordinator> _coordinator = new();
    private readonly InMemoryStateRepository _states = new();

    [Fact]
    public async Task KnowledgeFailure_StopsAtRetrieval_AndTheResumesKeepTheTriage()
    {
        Setup(AgentNames.Triage, Triaged());
        _coordinator.SetupSequence(c => c.ExecuteAgentAsync(AgentNames.Knowledge, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failed(AgentFailureMode.Transient))
            .ReturnsAsync(Failed(AgentFailureMode.Transient))
            .ReturnsAsync(Grounded());
        Setup(AgentNames.Drafting, new AgentResult { Response = "Here is how to restore access." });
        var workflow = CreateWorkflow();

        // Each transient failure stops the run at KnowledgeRetrieval with a time to resume; nothing retries within a request.
        var stopped = await workflow.ExecuteAsync(Ticket.Id, "engineer-ana", "locked out");
        var stoppedAgain = await workflow.ResumeAsync(stopped.WorkflowId, "engineer-ana");
        var result = await workflow.ResumeAsync(stopped.WorkflowId, "engineer-ana");

        Assert.Equal((WorkflowStep.KnowledgeRetrieval, false), (stopped.FinalStep, stopped.IsSuccess));
        Assert.NotNull(stopped.RetryAfter);
        Assert.Equal(WorkflowStep.KnowledgeRetrieval, stoppedAgain.FinalStep);
        Assert.Equal(WorkflowStep.Completed, result.FinalStep);
        Assert.Null(result.RetryAfter);
        Assert.Equal("Here is how to restore access.", result.FinalResponse);
        Assert.Equal(Citation.ArticleId, Assert.Single(result.Citations).ArticleId);

        // Triage ran once in all three requests; each request ran the knowledge step once.
        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Triage, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Knowledge, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task TransientFailures_BeyondMaxRetries_HandTheTicketToAPerson()
    {
        Setup(AgentNames.Triage, Triaged());
        Setup(AgentNames.Knowledge, Failed(AgentFailureMode.Transient));
        Setup(AgentNames.Escalation, new AgentResult
        {
            Response = "Escalated",
            Artifacts = { [AgentArtifactKeys.EscalationDraft] = new EscalationDraft("Login loop", "Team blocked", "Reset, sign in", "High") }
        });
        var workflow = CreateWorkflow();

        var first = await workflow.ExecuteAsync(Ticket.Id, "engineer-ana", "locked out");
        await workflow.ResumeAsync(first.WorkflowId, "engineer-ana");
        var last = await workflow.ResumeAsync(first.WorkflowId, "engineer-ana");

        Assert.True(last.EscalationRequired);
        Assert.Equal(WorkflowStep.Completed, last.FinalStep);
        Assert.Equal(AgentNames.Knowledge, _states.Saved.Last().GetArtifact<string>(WorkflowArtifactKeys.HandoffFrom));
        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Drafting, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FailedSave_AfterAStop_LeavesNoTimeToResume()
    {
        Setup(AgentNames.Triage, Triaged());
        Setup(AgentNames.Knowledge, Failed(AgentFailureMode.Transient));
        _states.FailNextStopSave = true;

        // The stop is recorded, but its save fails: the run ends as Failed, which no resume continues.
        var result = await CreateWorkflow().ExecuteAsync(Ticket.Id, "engineer-ana", "locked out");

        Assert.Equal(WorkflowStep.Failed, result.FinalStep);
        Assert.Null(result.RetryAfter);
        Assert.False(_states.Saved.Last().HasArtifact(WorkflowArtifactKeys.RetryAfter));
    }

    [Fact]
    public async Task UngroundedKnowledge_RunsEscalationOnce()
    {
        Setup(AgentNames.Triage, Triaged());
        Setup(AgentNames.Knowledge, new AgentResult { Response = "No sources.", RequiresHandoff = true, NextAgent = AgentNames.Escalation });
        Setup(AgentNames.Escalation, new AgentResult
        {
            Response = "Escalated",
            Artifacts = { [AgentArtifactKeys.EscalationDraft] = new EscalationDraft("Login loop", "Team blocked", "Reset, sign in", "High") }
        });

        var result = await CreateWorkflow().ExecuteAsync(Ticket.Id, "engineer-ben", "locked out");

        Assert.True(result.EscalationRequired);
        Assert.Equal("Login loop", result.FinalResponse);
        Assert.Equal(AgentNames.Knowledge, _states.Saved.Last().GetArtifact<string>(WorkflowArtifactKeys.HandoffFrom));
        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Escalation, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Drafting, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnrecoverableFailure_StopsTheRunWithTheError()
    {
        Setup(AgentNames.Triage, Failed(AgentFailureMode.Unrecoverable));

        var result = await CreateWorkflow().ExecuteAsync(Ticket.Id, "engineer-ana", "locked out");

        Assert.Equal(WorkflowStep.Failed, result.FinalStep);
        Assert.StartsWith(AgentNames.Triage, result.ErrorMessage);
        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Knowledge, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResumeAsync_ContinuesFromTheStoredStep()
    {
        // A run that stopped after retrieval: the stored state already holds the knowledge outcome.
        var stored = new WorkflowState { TicketId = Ticket.Id, ConversationId = Guid.NewGuid(), RequesterId = "engineer-ana" }
            .WithArtifact(WorkflowArtifactKeys.Knowledge, new KnowledgeOutcome("Answer", true, [Citation], "locked out"))
            .WithStep(WorkflowStep.Drafting);
        await _states.SaveAsync(stored);
        Setup(AgentNames.Drafting, new AgentResult { Response = "Draft" });

        var result = await CreateWorkflow().ResumeAsync(stored.WorkflowId, "engineer-ana");

        Assert.Equal(WorkflowStep.Completed, result.FinalStep);
        Assert.Single(result.Citations);
        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Triage, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Never);
        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Knowledge, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResumeAsync_ByAnotherEngineer_ReportsTheWorkflowAsMissing()
    {
        var stored = new WorkflowState { TicketId = Ticket.Id, ConversationId = Guid.NewGuid(), RequesterId = "engineer-ana" }
            .WithStep(WorkflowStep.Drafting);
        await _states.SaveAsync(stored);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateWorkflow().ResumeAsync(stored.WorkflowId, "engineer-ben"));

        _coordinator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Drafting_ReceivesTheKnowledgeStepsCitedAnswer()
    {
        Setup(AgentNames.Triage, Triaged());
        Setup(AgentNames.Knowledge, Grounded());
        Setup(AgentNames.Drafting, new AgentResult { Response = "Draft" });

        await CreateWorkflow().ExecuteAsync(Ticket.Id, "engineer-ana", "locked out");

        _coordinator.Verify(c => c.ExecuteAgentAsync(AgentNames.Drafting,
            It.Is<AgentContext>(context => context.Knowledge != null && context.Knowledge.Answer == "Clear the lockout."
                && context.Knowledge.Sources.Single().ArticleId == Citation.ArticleId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SqlRepository_ReadsTypedArtifactsBack()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<CopilotDbContext>().UseSqlite(connection).Options;
        var state = new WorkflowState { TicketId = Ticket.Id, RequesterId = "engineer-ana" }
            .WithArtifact(WorkflowArtifactKeys.Knowledge, new KnowledgeOutcome("Answer", true, [Citation], "query"))
            .WithStep(WorkflowStep.Drafting);

        await using (var context = new CopilotDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            await new SqlWorkflowStateRepository(context).SaveAsync(state);
        }

        await using (var context = new CopilotDbContext(options))
        {
            var loaded = await new SqlWorkflowStateRepository(context).GetAsync(state.WorkflowId);
            var knowledge = loaded!.GetArtifact<KnowledgeOutcome>(WorkflowArtifactKeys.Knowledge);
            Assert.Equal(WorkflowStep.Drafting, loaded.CurrentStep);
            Assert.Equal(Citation.Excerpt, Assert.Single(knowledge!.Citations).Excerpt);
            Assert.Equal("engineer-ana", loaded.RequesterId);
        }
    }

    [Fact]
    public async Task SqlRepository_RejectsASaveFromAnOlderVersion()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<CopilotDbContext>().UseSqlite(connection).Options;
        WorkflowState saved;
        await using (var context = new CopilotDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            saved = await new SqlWorkflowStateRepository(context).SaveAsync(new WorkflowState { TicketId = Ticket.Id, RequesterId = "engineer-ana" });
        }

        // Two runners load version 1; the first save wins, and the second fails instead of overwriting it.
        await using var first = new CopilotDbContext(options);
        await using var second = new CopilotDbContext(options);
        var firstState = (await new SqlWorkflowStateRepository(first).GetAsync(saved.WorkflowId))!;
        var secondState = (await new SqlWorkflowStateRepository(second).GetAsync(saved.WorkflowId))!;
        var afterFirst = await new SqlWorkflowStateRepository(first).SaveAsync(firstState.WithStep(WorkflowStep.KnowledgeRetrieval));

        await Assert.ThrowsAsync<WorkflowConflictException>(() =>
            new SqlWorkflowStateRepository(second).SaveAsync(secondState.WithStep(WorkflowStep.Escalation)));
        Assert.Equal(1, saved.Version);
        Assert.Equal(2, afterFirst.Version);
        Assert.Equal(WorkflowStep.KnowledgeRetrieval, (await new SqlWorkflowStateRepository(second).GetAsync(saved.WorkflowId))!.CurrentStep);
    }

    [Fact]
    public async Task Workflow_OnTheOfflineAgents_CompletesAndCitesItsSources()
    {
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped<EscalationDraftService>());
        using var scope = host.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
            .ExecuteAsync(SampleData.PasswordTicketId, "engineer-ana", "locked out");

        Assert.Equal(WorkflowStep.Completed, result.FinalStep);
        Assert.False(result.EscalationRequired);
        Assert.NotEmpty(result.Citations);
        Assert.StartsWith("[Offline sample reply]", result.FinalResponse);
        Assert.Contains("an administrator can clear the lockout", result.FinalResponse);
    }

    private SupportTicketWorkflow CreateWorkflow()
    {
        var conversations = new Mock<IConversationRepository>();
        conversations.Setup(c => c.GetByTicketAsync(Ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CopilotConversation { Id = Guid.NewGuid(), TicketId = Ticket.Id });
        var support = new Mock<ISupportContext>();
        support.Setup(s => s.GetTicketAsync(Ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Ticket);
        var correlation = new Mock<ICorrelationContext>();
        correlation.SetupGet(c => c.CorrelationId).Returns("corr-1");

        return new SupportTicketWorkflow(_coordinator.Object, conversations.Object, _states, correlation.Object, support.Object,
            new WorkflowRetryPolicy(MaxRetries: 2, RetryAfter: TimeSpan.FromSeconds(30)));
    }

    private void Setup(string agentName, AgentResult result) =>
        _coordinator.Setup(c => c.ExecuteAgentAsync(agentName, It.IsAny<AgentContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);

    private static AgentResult Triaged() => new()
    {
        Response = "Password reset, high urgency.",
        Artifacts = { [AgentArtifactKeys.Intent] = "PasswordReset", [AgentArtifactKeys.Confidence] = 0.9 }
    };

    private static AgentResult Grounded() => new()
    {
        Response = "Clear the lockout.",
        Artifacts =
        {
            [AgentArtifactKeys.Citations] = new List<CitationReference> { Citation },
            [AgentArtifactKeys.SearchQuery] = "locked out",
            [AgentArtifactKeys.IsGrounded] = true
        }
    };

    private static AgentResult Failed(AgentFailureMode mode) => new()
    {
        Response = "failed",
        RequiresHandoff = mode != AgentFailureMode.Transient,
        Artifacts = { [AgentArtifactKeys.FailureMode] = mode }
    };

    private sealed class InMemoryStateRepository : IWorkflowStateRepository
    {
        private readonly Dictionary<Guid, WorkflowState> _states = new();

        public List<WorkflowState> Saved { get; } = [];

        // Set by a test: the next save of a stopped run throws, as a locked database file would.
        public bool FailNextStopSave { get; set; }

        public Task<WorkflowState?> GetAsync(Guid workflowId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_states.GetValueOrDefault(workflowId));

        // Behaves like the SQL repository: a save from an older version fails.
        public Task<WorkflowState> SaveAsync(WorkflowState state, CancellationToken cancellationToken = default)
        {
            if (FailNextStopSave && state.HasArtifact(WorkflowArtifactKeys.RetryAfter))
            {
                FailNextStopSave = false;
                throw new InvalidOperationException("The database file is locked.");
            }

            if (_states.TryGetValue(state.WorkflowId, out var stored) && stored.Version != state.Version)
            {
                throw new WorkflowConflictException($"Workflow {state.WorkflowId} has a newer version.");
            }

            var saved = state with { Version = state.Version + 1 };
            _states[state.WorkflowId] = saved;
            Saved.Add(saved);
            return Task.FromResult(saved);
        }
    }
}
