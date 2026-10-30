using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Api;

// Two of the steps as an Agent Framework workflow: a sequential pipeline of agents with a
// checkpoint after every step, stored as JSON files, and a resume from the first checkpoint.
public static class AgentWorkflowExample
{
    public static async Task RunAsync(IServiceProvider services, string checkpointFolder, string engineerId, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var ticket = await provider.GetRequiredService<ISupportContext>().GetTicketAsync(SampleData.PasswordTicketId, cancellationToken)
            ?? throw new InvalidOperationException("The sample ticket is missing.");

        using var store = new FileSystemJsonCheckpointStore(new DirectoryInfo(checkpointFolder));
        CheckpointManager checkpoints = CheckpointManager.CreateJson(store);
        List<ChatMessage> input = [new(ChatRole.User, $"{ticket.Subject}\n{ticket.Description}")];

        using var first = BuildWorkflow(provider, ticket, engineerId);
        CheckpointInfo afterTriage;
        await using (Run run = await InProcessExecution.RunAsync(first.Workflow, input, checkpoints, $"ticket-{ticket.Id:N}", cancellationToken))
        {
            var output = run.OutgoingEvents.OfType<WorkflowOutputEvent>().Select(e => e.As<List<ChatMessage>>()).LastOrDefault() ?? [];
            Console.WriteLine($"Agent Framework workflow: {await run.GetStatusAsync(cancellationToken)}, {run.Checkpoints.Count} checkpoints, " +
                $"output of {output.Count} messages; {first.Describe()}");
            afterTriage = run.Checkpoints[0];
        }

        // The same workflow built again, as a new process would build it, resumes after the triage step:
        // only the knowledge step runs.
        using var second = BuildWorkflow(provider, ticket, engineerId);
        await using Run resumed = await InProcessExecution.ResumeAsync(second.Workflow, afterTriage, checkpoints, cancellationToken);
        Console.WriteLine($"Resumed from the first checkpoint: {await resumed.GetStatusAsync(cancellationToken)}; {second.Describe()}");
    }

    private static SupportTicketPipeline BuildWorkflow(IServiceProvider provider, SupportTicket ticket, string engineerId)
    {
        var tools = provider.GetRequiredService<AgentToolRegistry>();
        var factory = provider.GetRequiredService<SupportAgentFactory>();

        // The tools keep their boundary: the ticket and the engineer come from the context. The example keeps
        // no conversation, so it has no conversation ID.
        var context = new AgentToolContext(provider.GetRequiredService<ICorrelationContext>().CorrelationId, Guid.Empty, null, ticket, engineerId);
        var classify = new AgentToolFunction(tools.Resolve(TicketClassificationTool.ToolName), context);
        var search = new AgentToolFunction(tools.Resolve(KnowledgeRetrievalTool.ToolName), context);

        var triage = factory.Create("TriageStep", "Call classify_ticket, then state the classification in one sentence.", [classify]);
        var knowledge = factory.Create("KnowledgeStep",
            "Call search_knowledge_base with a short search query made from the ticket, then report the answer it returns.", [search]);

        // Each agent receives the conversation so far and adds its own messages to it.
        return new SupportTicketPipeline(AgentWorkflowBuilder.BuildSequential("support-ticket", [triage, knowledge]), classify, search, [triage, knowledge]);
    }

    // Disposing the pipeline disposes its agents' telemetry wrappers (Chapter 10).
    private sealed record SupportTicketPipeline(Workflow Workflow, AgentToolFunction Classify, AgentToolFunction Search,
        IReadOnlyList<OpenTelemetryAgent> Agents) : IDisposable
    {
        public string Describe() =>
            $"classify_ticket called {Classify.Calls.Count} times, search_knowledge_base called {Search.Calls.Count} times";

        public void Dispose()
        {
            foreach (var agent in Agents)
            {
                agent.Dispose();
            }
        }
    }
}
