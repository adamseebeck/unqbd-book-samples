using System.Runtime.CompilerServices;
using System.Text;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

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
        Console.WriteLine($"Contoso Support Copilot: Chapter 6 check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        if (chat?.ProviderName == "Offline")
        {
            Console.WriteLine("Offline provider: answers are local sample text and token counts are estimates.");
        }

        // Outside an HTTP request no middleware runs, so the check sets the correlation ID itself.
        services.GetRequiredService<ICorrelationContext>().SetCorrelationId("chapter06-check");

        var turnsBefore = await CountTurnsAsync(services);

        // Two turns in two scopes, like two HTTP requests: the second loads and extends the conversation.
        var first = await ProcessAsync(services, "The customer still cannot sign in after the reset.");
        var second = await ProcessAsync(services, "The mobile app shows the same lockout message.");
        Console.WriteLine($"Turn 1: {first.Intent.Intent} | {Shorten(first.Response)}");
        Console.WriteLine($"Turn 2: {second.Intent.Intent} | {Shorten(second.Response)}");

        var turnsAfter = await CountTurnsAsync(services);
        Console.WriteLine($"Conversation: one conversation for both turns: {first.ConversationId == second.ConversationId}, {turnsAfter - turnsBefore} turns added ({turnsAfter} in total)");

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var runs = await provider.GetRequiredService<IAgentRunRepository>().GetByConversationAsync(second.ConversationId);
        foreach (var run in runs.Take(2))
        {
            Console.WriteLine($"Run: {run.AgentName} | model {run.ModelId} | tokens {run.InputTokens}/{run.OutputTokens} | fallback {run.IsFallback} | correlation {run.CorrelationId}");
        }

        var summary = await provider.GetRequiredService<SupportWorkflowService>().SummarizeTicketAsync(SampleData.PaymentsTicketId);
        Console.WriteLine($"Summary: {Shorten(summary.Summary)} ({summary.InputTokens}/{summary.OutputTokens} tokens)");

        var staleSaveRejected = await CheckConcurrentSaveAsync(services);
        var fallbacksWork = await CheckFallbacksAsync(provider);
        var outageAnswered = await CheckOutageAsync(provider);
        if (first.ConversationId != second.ConversationId || turnsAfter - turnsBefore != 2
            || first.CorrelationId != "chapter06-check" || !staleSaveRejected || !fallbacksWork || !outageAnswered)
        {
            Console.WriteLine("Check failed.");
            return 1;
        }

        Console.WriteLine("Check passed.");
        return 0;
    }

    private static async Task<SupportWorkflowResult> ProcessAsync(IServiceProvider services, string userInput)
    {
        using var scope = services.CreateScope();
        var workflow = scope.ServiceProvider.GetRequiredService<SupportWorkflowService>();
        return await workflow.ProcessTicketAsync(SampleData.PasswordTicketId, userInput);
    }

    private static async Task<int> CountTurnsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var conversation = await scope.ServiceProvider.GetRequiredService<IConversationRepository>()
            .GetByTicketAsync(SampleData.PasswordTicketId);
        return conversation?.Turns.Count ?? 0;
    }

    // Two requests load the same turn count; the database rejects the save that comes second.
    private static async Task<bool> CheckConcurrentSaveAsync(IServiceProvider services)
    {
        using var firstScope = services.CreateScope();
        using var secondScope = services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IConversationRepository>();
        var second = secondScope.ServiceProvider.GetRequiredService<IConversationRepository>();
        var loadedByFirst = await first.GetByTicketAsync(SampleData.PasswordTicketId);
        var loadedBySecond = await second.GetByTicketAsync(SampleData.PasswordTicketId);
        if (loadedByFirst is null || loadedBySecond is null)
        {
            return false;
        }

        await first.SaveAsync(NextTurn(loadedByFirst, "A note saved by one request."));
        try
        {
            await second.SaveAsync(NextTurn(loadedBySecond, "A note saved by another request at the same time."));
            Console.WriteLine("Concurrent save: both saves were accepted.");
            return false;
        }
        catch (ConversationConflictException ex)
        {
            Console.WriteLine($"Concurrent save: the later save was rejected. {ex.Message}");
            return true;
        }
    }

    private static CopilotConversation NextTurn(CopilotConversation conversation, string userInput) => conversation with
    {
        CurrentTurn = conversation.CurrentTurn + 1,
        UpdatedAt = DateTime.UtcNow,
        Turns = [.. conversation.Turns, new ConversationTurn { TurnNumber = conversation.CurrentTurn + 1, UserInput = userInput, Timestamp = DateTime.UtcNow }]
    };

    // The decorator over a drafting service that fails on purpose.
    private static async Task<bool> CheckFallbacksAsync(IServiceProvider provider)
    {
        var ticket = await provider.GetRequiredService<ISupportContext>().GetTicketAsync(SampleData.ApiTicketId)
            ?? throw new InvalidOperationException("The sample ticket is missing; delete contoso-support.db and run again.");
        var options = new FallbackOptions();
        var fallback = new FallbackDraftingService(
            new FailingDraftingService(),
            provider.GetRequiredService<ILogger<FallbackDraftingService>>(),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(options));

        var reply = await fallback.DraftReplyAsync(ticket, new DraftingOptions());
        Console.WriteLine($"Fallback reply: {reply.Text} (fallback: {reply.IsFallback})");

        var streamed = new StringBuilder();
        await foreach (var chunk in fallback.DraftReplyStreamingAsync(ticket, new DraftingOptions()))
        {
            streamed.Append(chunk);
        }

        Console.WriteLine($"Fallback stream: {streamed.ToString().ReplaceLineEndings(" | ")}");
        return reply.IsFallback && streamed.ToString().EndsWith(options.FallbackMessage);
    }

    // An outage: intent extraction and drafting both fail, and the request still gets a usable reply.
    private static async Task<bool> CheckOutageAsync(IServiceProvider provider)
    {
        var runs = provider.GetRequiredService<IAgentRunRepository>();
        var workflow = new SupportWorkflowService(
            new FailingIntentExtractor(),
            new FallbackDraftingService(
                new FailingDraftingService(),
                provider.GetRequiredService<ILogger<FallbackDraftingService>>(),
                new MemoryCache(new MemoryCacheOptions()),
                Options.Create(new FallbackOptions())),
            provider.GetRequiredService<ISupportChatClient>(),
            provider.GetRequiredService<ISupportContext>(),
            provider.GetRequiredService<ICorrelationContext>(),
            provider.GetRequiredService<IConversationRepository>(),
            runs);

        var result = await workflow.ProcessTicketAsync(SampleData.ApiTicketId, "Why does the nightly export fail?");
        var run = (await runs.GetByConversationAsync(result.ConversationId))[0];
        Console.WriteLine($"Outage: intent {result.Intent.Intent} | {Shorten(result.Response)} | run fallback {run.IsFallback} | {run.ErrorMessage}");
        return result.Intent.Intent == "ManualReview" && run.IsFallback && run.TicketId == SampleData.ApiTicketId;
    }

    private static string Shorten(string text) => text.Length <= 90 ? text : text[..90] + "...";

    // Fails like intent extraction while the circuit is open.
    private sealed class FailingIntentExtractor : IIntentExtractor
    {
        public Task<SupportIntent> ExtractAsync(string ticketDescription, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The AI service is temporarily unavailable: too many recent calls failed.");
    }

    // Fails on purpose: the complete draft at once, the streamed draft after its first words.
    private sealed class FailingDraftingService : IDraftingService
    {
        public Task<DraftReply> DraftReplyAsync(SupportTicket ticket, DraftingOptions options, CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("Simulated connection failure");

        public async IAsyncEnumerable<string> DraftReplyStreamingAsync(
            SupportTicket ticket,
            DraftingOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "Thank you for contacting Contoso Support. ";
            await Task.Delay(10, cancellationToken);
            throw new HttpRequestException("Simulated connection failure");
        }
    }
}
