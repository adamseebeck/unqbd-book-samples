using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this activity's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        Console.WriteLine("Contoso Support Copilot: Chapter 7 activity check (embedding cache)");

        var client = services.GetRequiredService<IEmbeddingClient>();
        Console.WriteLine($"IEmbeddingClient resolves to {client.GetType().Name}");

        const string text = "Reset links expire after one hour.";
        var first = await client.GenerateAsync(text);
        var repeated = await client.GenerateAsync(text);
        var shortened = await client.GenerateAsync(text, new EmbeddingOptions { Dimensions = 128 });

        Console.WriteLine($"First call: {first.TokenCount} tokens; same text again: {repeated.TokenCount} tokens, same vector: {first.Vector.SequenceEqual(repeated.Vector)}");
        Console.WriteLine($"Same text with Dimensions = 128: {shortened.TokenCount} tokens, {shortened.Vector.Length} dimensions (its own cache entry)");

        var passed = client is CachingEmbeddingClient
            && first.TokenCount > 0
            && repeated.TokenCount == 0
            && first.Vector.SequenceEqual(repeated.Vector)
            && shortened.TokenCount > 0
            && shortened.Vector.Length == 128;

        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }
}
