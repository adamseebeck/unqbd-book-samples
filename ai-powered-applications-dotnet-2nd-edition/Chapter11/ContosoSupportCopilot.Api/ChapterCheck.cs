using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this chapter's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        var chat = services.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();
        Console.WriteLine($"Contoso Support Copilot: Chapter 11 check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        var passed = true;

        // The sample articles must be in the vector store; with Azure AI Search, this fills the index.
        await services.InitializeKnowledgeIndexAsync();

        // 1. The full path: triage, knowledge retrieval and drafting for an engineer with access.
        using (var scope = services.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
                .ExecuteAsync(SampleData.PasswordTicketId, "engineer-ana", "Customer is locked out after a reset");
            Print("Password ticket for engineer-ana", result);
            passed &= result is { IsSuccess: true, EscalationRequired: false, FinalStep: WorkflowStep.Completed } && result.Citations.Count > 0;
        }

        // 2. No grounded sources for this engineer: the knowledge step hands off and escalation runs once.
        using (var scope = services.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
                .ExecuteAsync(SampleData.ApiTicketId, "engineer-ben", "Nightly sync fails with 429");
            Print("API ticket for engineer-ben (Payments only)", result);
            var state = await scope.ServiceProvider.GetRequiredService<IWorkflowStateRepository>().GetAsync(result.WorkflowId);
            Console.WriteLine($"Handoff from: {state?.GetArtifact<string>(WorkflowArtifactKeys.HandoffFrom)}");
            passed &= result is { IsSuccess: true, EscalationRequired: true } && state?.GetArtifact<string>(WorkflowArtifactKeys.HandoffFrom) == AgentNames.Knowledge;
        }

        // 3. Restart recovery: the host stops during drafting; the stored state resumes there.
        Guid interrupted;
        using (var scope = services.CreateScope())
        {
            using var stop = new CancellationTokenSource();
            var coordinator = scope.ServiceProvider.GetRequiredService<IAgentCoordinator>();
            await coordinator.RegisterAgentAsync(AgentNames.Drafting, new StoppingAgent(stop));
            try
            {
                await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
                    .ExecuteAsync(SampleData.PasswordTicketId, "engineer-ana", "Second attempt", stop.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Run stopped during drafting.");
            }

            var stored = await scope.ServiceProvider.GetRequiredService<CopilotDbContext>().WorkflowStates.AsNoTracking()
                .Where(w => w.TicketId == SampleData.PasswordTicketId).OrderByDescending(w => w.StartedAt).FirstAsync();
            interrupted = stored.WorkflowId;
            Console.WriteLine($"Stored state: step {stored.CurrentStep}, artifacts {stored.ArtifactsJson.Length} characters");
            passed &= stored.CurrentStep == nameof(WorkflowStep.Drafting);
        }

        using (var scope = services.CreateScope())
        {
            var runs = scope.ServiceProvider.GetRequiredService<IAgentRunRepository>();
            var conversation = await scope.ServiceProvider.GetRequiredService<IConversationRepository>().GetByTicketAsync(SampleData.PasswordTicketId);
            var knowledgeRunsBefore = (await runs.GetByConversationAsync(conversation!.Id)).Count(r => r.AgentName == AgentNames.Knowledge);

            // Only the engineer who started the workflow may resume it; for anyone else it does not exist.
            var workflow = scope.ServiceProvider.GetRequiredService<ISupportWorkflow>();
            var otherEngineer = await CatchAsync(() => workflow.ResumeAsync(interrupted, "engineer-ben"));
            Console.WriteLine($"Resume by engineer-ben: {(otherEngineer is ArgumentException ? "not found" : "allowed")}");
            passed &= otherEngineer is ArgumentException;

            var resumed = await workflow.ResumeAsync(interrupted, "engineer-ana");
            var knowledgeRunsAfter = (await runs.GetByConversationAsync(conversation.Id)).Count(r => r.AgentName == AgentNames.Knowledge);
            Print("Resumed run", resumed);
            Console.WriteLine($"KnowledgeAgent runs for this conversation: {knowledgeRunsBefore} before the resume, {knowledgeRunsAfter} after");
            passed &= resumed.FinalStep == WorkflowStep.Completed && knowledgeRunsAfter == knowledgeRunsBefore && resumed.Citations.Count > 0;
        }

        // A second runner of the same workflow: its save fails instead of overwriting the first runner's progress.
        using (var scope = services.CreateScope())
        {
            var states = scope.ServiceProvider.GetRequiredService<IWorkflowStateRepository>();
            var loaded = (await states.GetAsync(interrupted))!;
            await states.SaveAsync(loaded with { LastUpdatedAt = DateTime.UtcNow });
            var stale = await CatchAsync(() => states.SaveAsync(loaded with { LastUpdatedAt = DateTime.UtcNow }));
            Console.WriteLine($"Save from version {loaded.Version} after another save: {(stale is WorkflowConflictException ? "conflict" : "accepted")}");
            passed &= stale is WorkflowConflictException;
        }

        // 4. Checkpointed retry: a transient failure stops the run at its step, with a time to resume, and the
        // resume runs only that step, not the triage before it.
        Guid paused;
        using (var scope = services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAgentCoordinator>().RegisterAgentAsync(AgentNames.Knowledge, new UnavailableAgent());
            var result = await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
                .ExecuteAsync(SampleData.PasswordTicketId, "engineer-ana", "Third attempt");
            paused = result.WorkflowId;
            Console.WriteLine($"Transient knowledge failure: step {result.FinalStep}, success {result.IsSuccess}, resume after: {(result.RetryAfter is null ? "none" : "set")}");
            passed &= result is { FinalStep: WorkflowStep.KnowledgeRetrieval, IsSuccess: false, RetryAfter: not null };
        }

        using (var scope = services.CreateScope())
        {
            var resumed = await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>().ResumeAsync(paused, "engineer-ana");
            Print("Resumed after the transient failure", resumed);
            passed &= resumed is { FinalStep: WorkflowStep.Completed, RetryAfter: null } && resumed.Citations.Count > 0;
        }

        // 5. The same steps as an Agent Framework workflow with checkpoints.
        var checkpointFolder = Path.Combine(Path.GetTempPath(), $"contoso-checkpoints-{Guid.NewGuid():N}");
        try
        {
            await AgentWorkflowExample.RunAsync(services, checkpointFolder, "engineer-ana");
        }
        finally
        {
            if (Directory.Exists(checkpointFolder))
            {
                Directory.Delete(checkpointFolder, recursive: true);
            }
        }

        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }

    // Runs an action and returns what it threw, or null.
    private static async Task<Exception?> CatchAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static void Print(string label, WorkflowResult result) =>
        Console.WriteLine($"{label}: {result.FinalStep}, success {result.IsSuccess}, escalation {result.EscalationRequired}, " +
            $"citations {result.Citations.Count}; response: {result.FinalResponse}");

    // Stands in for a knowledge agent whose model calls failed after the Chapter 4 pipeline's retries, which the
    // real agent reports as a transient failure. A failed draft never does: Chapter 6's fallback answers instead.
    private sealed class UnavailableAgent : IAgent
    {
        public string Name => AgentNames.Knowledge;

        public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(AgentResults.Failure(AgentNames.Knowledge, AgentFailureMode.Transient));
    }

    // Stands in for a host that shuts down while the drafting agent is working.
    private sealed class StoppingAgent(CancellationTokenSource stop) : IAgent
    {
        public string Name => AgentNames.Drafting;

        public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
        {
            stop.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new AgentResult());
        }
    }
}
