using System.Globalization;
using System.Text.Json;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Polly.CircuitBreaker;

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
        Console.WriteLine($"Contoso Support Copilot: Chapter 10 check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        var passed = true;

        // 1. The framework primitives on their own.
        await AgentPrimitivesExample.RunAsync(services);

        // The sample articles must be in the vector store; with Azure AI Search, this fills the index.
        await services.InitializeKnowledgeIndexAsync();

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var copilot = provider.GetRequiredService<SupportCopilotService>();
        var sessions = provider.GetRequiredService<IAgentSessionRepository>();
        var conversations = provider.GetRequiredService<IConversationRepository>();

        // 2. Triage: the agent calls both tools, and the artifacts come from their typed results.
        var triage = await copilot.TriageTicketAsync(SampleData.PasswordTicketId);
        Console.WriteLine($"Triage: {triage.Response}");
        Console.WriteLine($"Triage artifacts: intent {triage.Artifacts.GetValueOrDefault(AgentArtifactKeys.Intent)}, " +
            $"urgency {triage.Artifacts.GetValueOrDefault(AgentArtifactKeys.Urgency)}, " +
            $"product area {triage.Artifacts.GetValueOrDefault(AgentArtifactKeys.ProductArea)}, " +
            $"confidence {F(triage.Artifacts.GetValueOrDefault(AgentArtifactKeys.Confidence))}, " +
            $"priority score {F(triage.Artifacts.GetValueOrDefault(AgentArtifactKeys.PriorityScore))}; handoff {triage.RequiresHandoff}");
        passed &= !triage.RequiresHandoff && triage.Artifacts.ContainsKey(AgentArtifactKeys.PriorityScore);

        // 3. Knowledge for an engineer assigned to Authentication: a grounded answer with citations.
        var knowledge = await copilot.FindKnowledgeAsync(SampleData.PasswordTicketId, "engineer-ana");
        var citations = knowledge.Artifacts.GetValueOrDefault(AgentArtifactKeys.Citations) as IReadOnlyList<CitationReference> ?? [];
        Console.WriteLine($"Knowledge for engineer-ana: grounded {knowledge.Artifacts.GetValueOrDefault(AgentArtifactKeys.IsGrounded)}, " +
            $"cites: {string.Join("; ", citations.Select(c => c.ArticleTitle).Distinct())}");
        Console.WriteLine($"Answer: {knowledge.Response}");
        passed &= !knowledge.RequiresHandoff && citations.Count > 0;

        // 4. The same conversation again: the agent continues its stored session.
        var conversation = await conversations.GetByTicketAsync(SampleData.PasswordTicketId);
        var firstTurn = await sessions.GetAsync(conversation!.Id, AgentNames.Knowledge, "engineer-ana");
        var before = await CountSessionMessagesAsync(provider, firstTurn);
        await copilot.FindKnowledgeAsync(SampleData.PasswordTicketId, "engineer-ana");
        var secondTurn = await sessions.GetAsync(conversation.Id, AgentNames.Knowledge, "engineer-ana");
        var after = await CountSessionMessagesAsync(provider, secondTurn);
        Console.WriteLine($"KnowledgeAgent session for this conversation: {before} messages after the first turn, {after} after the second");
        passed &= after > before && before > 0;

        // The session's Version is a concurrency token: a save that started from the first turn's version
        // cannot replace the second turn.
        var conflict = false;
        try
        {
            await sessions.SaveAsync(firstTurn!);
        }
        catch (AgentSessionConflictException)
        {
            conflict = true;
        }

        Console.WriteLine($"Saving the session from version {firstTurn!.Version} after version {secondTurn!.Version}: conflict {conflict}");
        passed &= conflict && secondTurn.Version == firstTurn.Version + 1;

        // 5. An engineer without access to Authentication gets no sources, so the agent hands off.
        // Sessions are kept per engineer, so engineer-ben's run never sees engineer-ana's results.
        var denied = await copilot.FindKnowledgeAsync(SampleData.PasswordTicketId, "engineer-ben");
        Console.WriteLine($"Knowledge for engineer-ben (Payments only): grounded {denied.Artifacts.GetValueOrDefault(AgentArtifactKeys.IsGrounded)}, " +
            $"handoff {denied.RequiresHandoff} to {denied.NextAgent}");
        passed &= denied.RequiresHandoff && denied.NextAgent == AgentNames.Escalation;

        // 6. Drafting through its tool, with the model and tokens from the reply: first from the ticket alone,
        // then with the knowledge agent's cited answer handed over as the draft's facts.
        var draft = await copilot.DraftReplyAsync(SampleData.PasswordTicketId);
        Console.WriteLine($"Draft from the ticket alone: {draft}");
        var grounded = await copilot.DraftGroundedReplyAsync(SampleData.PasswordTicketId, "engineer-ana");
        Console.WriteLine($"Draft from the knowledge answer: {grounded.Response}");
        passed &= !grounded.RequiresHandoff && grounded.Response.Length > 0 && !grounded.Response.Contains("[Article:", StringComparison.Ordinal);

        // 7. Every run left a record with the model from the response, never a hard-coded name.
        var runs = await provider.GetRequiredService<IAgentRunRepository>().GetByConversationAsync(conversation.Id);
        foreach (var run in runs.OrderBy(r => r.Timestamp))
        {
            Console.WriteLine($"AgentRun {run.AgentName}: model {run.ModelId}, tokens {run.InputTokens}/{run.OutputTokens}, fallback {run.IsFallback}");
        }
        passed &= runs.Count >= 5 && runs.All(r => r.ModelId.Length > 0);

        // 8. Failure classification decides between retry and handoff. The chat client reports a time limit as
        // TimeoutException and an open circuit as InvalidOperationException around BrokenCircuitException.
        Exception[] failures =
        [
            new TimeoutException(), new InvalidOperationException("The circuit is open.", new BrokenCircuitException()),
            new UnauthorizedAccessException(), new InvalidOperationException()
        ];
        Console.WriteLine("Failure modes: " + string.Join(", ", failures.Select(e => $"{Describe(e)} -> {AgentFailureClassifier.Classify(e)}")));

        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }

    // Restores a stored session with a tool-less agent of the same name and counts its messages.
    private static async Task<int> CountSessionMessagesAsync(IServiceProvider provider, AgentSessionRecord? stored)
    {
        if (stored is null)
        {
            return 0;
        }

        using var agent = provider.GetRequiredService<SupportAgentFactory>().Create(AgentNames.Knowledge, "Count the messages.", []);
        var session = await agent.DeserializeSessionAsync(JsonSerializer.Deserialize<JsonElement>(stored.SessionJson));
        return agent.GetService<ChatClientAgent>()?.ChatHistoryProvider is InMemoryChatHistoryProvider history
            ? history.GetMessages(session).Count
            : 0;
    }

    private static string Describe(Exception exception) =>
        exception.InnerException is null ? exception.GetType().Name : $"{exception.GetType().Name}({exception.InnerException.GetType().Name})";

    private static string F(object? value) =>
        value is double number ? number.ToString("0.00", CultureInfo.InvariantCulture) : value?.ToString() ?? "none";
}
