using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
        var chat = provider.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();
        var embeddings = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var embeddingMetadata = embeddings.GetService<EmbeddingGeneratorMetadata>();

        Console.WriteLine($"Contoso Support Copilot: Chapter 2 check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        if (chat?.ProviderName == "Offline")
        {
            Console.WriteLine("Offline provider: answers are local sample text, not model output.");
        }

        // The checks behind /health: offline there is no call; a real provider gets one short request.
        var health = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();
        foreach (var (name, entry) in health.Entries)
        {
            Console.WriteLine($"Health {name}: {entry.Status} ({entry.Description})");
        }

        if (health.Status != HealthStatus.Healthy)
        {
            Console.WriteLine("Check stopped: the AI provider is unreachable. The API itself still starts, and /health reports the same status.");
            return 1;
        }

        var supportContext = provider.GetRequiredService<ISupportContext>();
        var openTickets = await supportContext.GetOpenTicketsAsync("Authentication");
        Console.WriteLine($"Database: {openTickets.Count} open Authentication ticket(s)");

        var copilot = provider.GetRequiredService<SupportCopilotService>();
        var summary = await copilot.SummarizeTicketAsync(SampleData.PasswordTicketId);
        Console.WriteLine($"Summary ({summary.InputTokens} input, {summary.OutputTokens} output tokens): {summary.Summary}");

        var draft = await copilot.DraftReplyAsync(SampleData.ApiTicketId);
        Console.WriteLine($"Semantic Kernel draft: {draft}");

        var vector = await embeddings.GenerateVectorAsync("Cannot sign in after password reset");
        Console.WriteLine($"Embedding: {vector.Length} dimensions from {embeddingMetadata?.ProviderName}");

        if (openTickets.Count == 0 || string.IsNullOrWhiteSpace(summary.Summary) || string.IsNullOrWhiteSpace(draft) || vector.Length == 0)
        {
            Console.WriteLine("Check failed: a layer returned no data.");
            return 1;
        }

        Console.WriteLine("Check passed.");
        return 0;
    }
}
