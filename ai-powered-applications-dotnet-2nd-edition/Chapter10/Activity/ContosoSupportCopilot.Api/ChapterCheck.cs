using System.Globalization;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this activity's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        var chat = services.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();
        Console.WriteLine($"Contoso Support Copilot: Chapter 10 activity check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        // The ticket's active conversation; Chapter 6 allows one per ticket, so a second run reuses it.
        var conversations = provider.GetRequiredService<IConversationRepository>();
        var conversation = await conversations.GetByTicketAsync(SampleData.PasswordTicketId);
        if (conversation is null)
        {
            conversation = new CopilotConversation { Id = Guid.NewGuid(), TicketId = SampleData.PasswordTicketId, CreatedAt = DateTime.UtcNow };
            await conversations.SaveAsync(conversation);
        }

        var ticket = await provider.GetRequiredService<ISupportContext>().GetTicketAsync(SampleData.PasswordTicketId);

        var result = await provider.GetRequiredService<IAgentCoordinator>().ExecuteAgentAsync(
            AgentNames.Escalation,
            new AgentContext { ConversationId = conversation.Id, CurrentTicket = ticket, Intent = "escalation" });

        var draft = result.Artifacts.GetValueOrDefault(AgentArtifactKeys.EscalationDraft) as EscalationDraft;
        var score = result.Artifacts.GetValueOrDefault(AgentArtifactKeys.PriorityScore) as double?;
        Console.WriteLine($"EscalationAgent: handoff {result.RequiresHandoff}; recommended priority {draft?.RecommendedPriority}; " +
            $"priority score {score?.ToString("0.00", CultureInfo.InvariantCulture)}; generated at set: {result.Artifacts.ContainsKey(AgentArtifactKeys.GeneratedAt)}");
        Console.WriteLine($"Summary: {draft?.Summary}");

        var run = (await provider.GetRequiredService<IAgentRunRepository>().GetByConversationAsync(conversation.Id))
            .Where(r => r.AgentName == AgentNames.Escalation)
            .MaxBy(r => r.Timestamp);
        Console.WriteLine($"AgentRun {run?.AgentName}: model {run?.ModelId}, fallback {run?.IsFallback}");

        var passed = !result.RequiresHandoff && draft is not null && score is not null && run is { IsFallback: false };
        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }
}
