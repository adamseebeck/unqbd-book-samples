using System.Text;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this chapter's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    private static readonly string[] SampleDescriptions =
    [
        "I reset my password this morning and now every sign in attempt says my account is locked. Our team is blocked.",
        "Our card was charged twice for the October invoice. Please refund the duplicate payment.",
        "Our nightly integration job gets HTTP 429 responses from the orders endpoint. How should we handle the rate limit?",
        "Something seems off with my account since yesterday."
    ];

    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var chat = provider.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();

        Console.WriteLine($"Contoso Support Copilot: Chapter 3 check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        if (chat?.ProviderName == "Offline")
        {
            Console.WriteLine("Offline provider: answers are local sample text and token counts are estimates.");
        }

        var extractor = provider.GetRequiredService<IIntentExtractor>();
        foreach (var description in SampleDescriptions)
        {
            var intent = await extractor.ExtractAsync(description);
            Console.WriteLine($"Intent: {intent.Intent} | Urgency: {intent.Urgency} | Area: {intent.ProductArea} | Confidence: {intent.ConfidenceScore:F2}");
        }

        var ticket = await provider.GetRequiredService<ISupportContext>().GetTicketAsync(SampleData.PasswordTicketId)
            ?? throw new InvalidOperationException("The sample ticket is missing; delete contoso-support.db and run again.");
        var drafting = provider.GetRequiredService<IDraftingService>();

        var streamed = new StringBuilder();
        var chunks = 0;
        await foreach (var text in drafting.DraftReplyStreamingAsync(ticket, new DraftingOptions()))
        {
            streamed.Append(text);
            chunks++;
        }

        Console.WriteLine($"Streaming draft ({chunks} chunks): {streamed}");

        var reply = await drafting.DraftReplyAsync(ticket, new DraftingOptions());
        Console.WriteLine($"Complete draft: {reply}");

        // Semantic Kernel shares the same IChatClient pipeline, logging middleware included.
        var kernelDraft = await provider.GetRequiredService<SupportCopilotService>().DraftReplyAsync(SampleData.ApiTicketId);
        Console.WriteLine($"Semantic Kernel draft: {kernelDraft}");

        if (chunks < 2 || string.IsNullOrWhiteSpace(reply) || string.IsNullOrWhiteSpace(kernelDraft))
        {
            Console.WriteLine("Check failed: drafting returned no streamed or complete text.");
            return 1;
        }

        Console.WriteLine("Check passed.");
        return 0;
    }
}
