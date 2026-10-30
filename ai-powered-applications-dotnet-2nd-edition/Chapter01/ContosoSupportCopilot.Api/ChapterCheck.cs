using ContosoSupportCopilot.Application.Services;
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
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var metadata = provider.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();

        Console.WriteLine($"Contoso Support Copilot: Chapter 1 check (chat provider: {metadata?.ProviderName}, model: {metadata?.DefaultModelId})");
        if (metadata?.ProviderName == "Offline")
        {
            Console.WriteLine("Offline provider: answers are local sample text, not model output.");
        }

        var copilot = provider.GetRequiredService<SupportCopilotService>();
        var summary = await copilot.SummarizeTicketAsync(SampleData.PasswordTicketId);
        Console.WriteLine($"Summary: {summary.Summary}");
        Console.WriteLine($"Tokens: {summary.InputTokens} input, {summary.OutputTokens} output");

        Console.Write("Streamed: ");
        var chatClient = provider.GetRequiredService<ISupportChatClient>();
        var streamedOutputTokens = 0;
        await foreach (var chunk in chatClient.CompleteStreamingAsync("Draft a one-sentence reply that thanks the customer."))
        {
            Console.Write(chunk.Content);
            if (chunk.IsComplete)
            {
                streamedOutputTokens = chunk.OutputTokens;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Streamed output tokens: {streamedOutputTokens}");

        if (string.IsNullOrWhiteSpace(summary.Summary) || summary.OutputTokens == 0 || streamedOutputTokens == 0)
        {
            Console.WriteLine("Check failed: the chat layer returned no content or no usage.");
            return 1;
        }

        Console.WriteLine("Check passed.");
        return 0;
    }
}
