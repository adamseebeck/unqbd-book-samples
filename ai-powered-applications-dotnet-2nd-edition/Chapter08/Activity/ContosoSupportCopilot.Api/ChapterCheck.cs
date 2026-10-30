using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Security;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this activity's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        Console.WriteLine("Contoso Support Copilot: Chapter 8 activity check (masking, cached authorization, conversation history)");
        await services.InitializeKnowledgeIndexAsync();
        var passed = true;

        // 1. Phone numbers and obfuscated email addresses.
        var masked = services.GetRequiredService<ISensitiveDataMasker>()
            .MaskSensitiveContent("Call +1 555 010 0199 or write to ana [at] contoso [dot] com.");
        Console.WriteLine($"Masked: {masked}");
        passed &= masked == "Call [PHONE_REDACTED] or write to [EMAIL_REDACTED].";

        // 2. Four turns in the API ticket's conversation, each in its own scope like an HTTP request.
        for (var turn = 1; turn <= 4; turn++)
        {
            using var turnScope = services.CreateScope();
            await turnScope.ServiceProvider.GetRequiredService<SupportWorkflowService>()
                .ProcessTicketAsync(SampleData.ApiTicketId, $"Follow-up question {turn} about the 429 responses");
        }

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var conversation = await provider.GetRequiredService<IConversationRepository>().GetByTicketAsync(SampleData.ApiTicketId);
        var history = conversation!.Turns.OrderBy(t => t.TurnNumber).TakeLast(3).ToList();
        var prompt = provider.GetRequiredService<RagPromptBuilder>().BuildGroundedPrompt(SupportPrompts.GroundedAnswer, "Subject: API returns 429", [], history);
        var turnNumbers = string.Join(", ", history.Select(t => t.TurnNumber));
        Console.WriteLine($"Conversation history in the prompt: turns {turnNumbers} of {conversation.Turns.Count}, in <conversation_history>: {prompt.Contains("<conversation_history>")}");
        passed &= history.Count == 3 && prompt.Split("<turn ").Length - 1 == 3;

        // 3. One grounded answer: the filter decides once per article, even for several chunks.
        var ticket = await provider.GetRequiredService<ISupportContext>().GetTicketAsync(SampleData.PasswordTicketId)
            ?? throw new InvalidOperationException("The sample ticket is missing; delete contoso-support.db and run again.");
        var answer = await provider.GetRequiredService<IRagOrchestrator>().GenerateGroundedResponseAsync(
            new RagRequest { TicketId = ticket.Id, Query = ticket.Subject, ProductArea = ticket.ProductArea, RequesterId = "engineer-ana", MaxResults = 3 },
            ticket);
        var filter = (CachingAuthorizationFilter)provider.GetRequiredService<IAuthorizationFilter>();
        Console.WriteLine($"Grounded answer: {answer.IsGrounded}, sources {answer.Citations.Count}; authorization checks {filter.CheckCount}, decisions computed {filter.CachedDecisionCount}");
        passed &= answer.IsGrounded && filter.CheckCount > filter.CachedDecisionCount;

        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }
}
