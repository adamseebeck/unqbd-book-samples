using System.Globalization;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Search;
using Microsoft.Extensions.AI;
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
        var embeddings = services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>().GetService<EmbeddingGeneratorMetadata>();
        var vectorOptions = services.GetRequiredService<IOptions<VectorStoreOptions>>().Value;
        Console.WriteLine($"Contoso Support Copilot: Chapter 7 check (chat provider: {chat?.ProviderName}, embedding model: {embeddings?.DefaultModelId}, vector store: {vectorOptions.Provider})");
        if (embeddings?.ProviderName == "Offline")
        {
            Console.WriteLine("Offline provider: embeddings are hashed word vectors, so texts that share words score as similar; a real model also matches meaning.");
        }

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var passed = true;

        // 1. Embeddings through the domain wrapper, at the default and at a shortened length.
        var embeddingClient = provider.GetRequiredService<IEmbeddingClient>();
        var full = await embeddingClient.GenerateAsync("The customer is locked out after a password reset");
        var shortened = await embeddingClient.GenerateAsync("The customer is locked out after a password reset", new EmbeddingOptions { Dimensions = 128 });
        Console.WriteLine($"Embedding: {full.Vector.Length} dimensions, {full.TokenCount} tokens, model {full.ModelId}; with Dimensions = 128: {shortened.Vector.Length} dimensions");
        passed &= shortened.Vector.Length == 128 && full.TokenCount > 0;

        // 2. Chunking the longest sample article.
        var supportContext = provider.GetRequiredService<ISupportContext>();
        var article = (await supportContext.GetArticlesAsync([SampleData.SignInErrorsArticleId])).Single();
        var options = new ChunkingOptions();
        var chunks = await provider.GetRequiredService<IArticleChunkingService>().ChunkAsync(article, options);
        Console.WriteLine($"Chunking \"{article.Title}\" ({article.Content.Length} characters): {chunks.Count} chunks of at most {options.MaxChunkSize} characters");
        foreach (var chunk in chunks)
        {
            Console.WriteLine($"  chunk {chunk.ChunkIndex}: {chunk.Content.Length} characters, starts \"{Start(chunk.Content)}\"");
        }

        var overlapCarried = chunks.Zip(chunks.Skip(1))
            .All(pair => pair.First.Content[^options.OverlapSize..].Contains(FirstWords(pair.Second.Content, 3)));
        Console.WriteLine($"Every chunk within the limit: {chunks.All(c => c.Content.Length <= options.MaxChunkSize)}; each chunk starts with the end of the previous one: {overlapCarried}");
        passed &= chunks.Count > 1 && chunks.All(c => c.Content.Length <= options.MaxChunkSize) && overlapCarried;

        // 3. Indexing again replaces the chunks: the store holds the same number before and after.
        var vectorSearch = provider.GetRequiredService<IVectorSearchService>();
        var query = await embeddingClient.GenerateAsync("The customer is locked out after a password reset");
        var indexed = await services.InitializeKnowledgeIndexAsync();
        var storedBefore = await CountStoredChunksAsync(vectorSearch, query.Vector, indexed);
        await services.InitializeKnowledgeIndexAsync();
        var storedAfter = await CountStoredChunksAsync(vectorSearch, query.Vector, indexed);
        Console.WriteLine($"Index: {indexed} chunks from the sample articles; stored before indexing again: {storedBefore}, after: {storedAfter}");
        passed &= storedBefore == indexed && storedAfter == indexed;

        // 4. Vector search: product area filter, publication filter, score floor.
        var request = new VectorSearchRequest { TopK = vectorOptions.TopK, MinScore = vectorOptions.MinScore, ProductAreaFilter = "Authentication" };
        var results = await vectorSearch.SearchAsync(query.Vector, request);
        Console.WriteLine($"Search \"The customer is locked out after a password reset\" in Authentication (MinScore {Format(request.MinScore)}):");
        foreach (var result in results)
        {
            Console.WriteLine($"  {Format(result.Score)} {result.ArticleTitle} (chunk {result.Chunk.ChunkIndex})");
        }

        passed &= results.Count > 0 && results.All(r => r.ProductArea == "Authentication") && results.Select(r => r.Chunk.Id).Distinct().Count() == results.Count;

        var refund = await embeddingClient.GenerateAsync("When does the customer get the refund?");
        var published = await vectorSearch.SearchAsync(refund.Vector, request with { ProductAreaFilter = "Payments" });
        var withDrafts = await vectorSearch.SearchAsync(refund.Vector, request with { ProductAreaFilter = "Payments", IncludeUnpublished = true });
        Console.WriteLine($"Refund question in Payments: {Titles(published)}; with unpublished articles: {Titles(withDrafts)}");
        passed &= published.All(r => r.ArticleId != SampleData.DraftRefundArticleId) && withDrafts.Any(r => r.ArticleId == SampleData.DraftRefundArticleId);

        var strict = await vectorSearch.SearchAsync(query.Vector, request with { MinScore = 0.95 });
        Console.WriteLine($"With MinScore 0.95: {strict.Count} results");
        passed &= strict.Count == 0;

        // 5. The repository behind ISupportContext: the knowledge search that earlier chapters call.
        var ticket = await supportContext.GetTicketAsync(SampleData.PasswordTicketId)
            ?? throw new InvalidOperationException("The sample ticket is missing; delete contoso-support.db and run again.");
        var articles = await supportContext.SearchKnowledgeBaseAsync(ticket.Subject, ticket.ProductArea);
        Console.WriteLine($"Knowledge search for \"{ticket.Subject}\": {string.Join("; ", articles.Select(a => $"{a.Title} (matching chunks: {a.Chunks.Count})"))}");
        passed &= articles.Count > 0 && articles.All(a => a.ProductArea == "Authentication" && a.Chunks.Count > 0);

        // 6. The Azure AI Search filter escapes a value that tries to widen the filter.
        var filter = AzureAISearchVectorService.BuildFilter(new VectorSearchRequest { ProductAreaFilter = "Payments' or isPublished eq false or productArea eq '" });
        Console.WriteLine($"Azure AI Search filter for a hostile product area: {filter}");
        passed &= filter == "isPublished eq true and productArea eq 'Payments'' or isPublished eq false or productArea eq '''";

        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }

    // Every stored chunk, whatever its score: a floor of -1 lets every cosine similarity through.
    // Azure AI Search makes uploaded chunks searchable shortly after the upload, so the count waits up to
    // ten seconds for the store to reach the expected number; the in-memory store answers at once.
    private static async Task<int> CountStoredChunksAsync(IVectorSearchService vectorSearch, float[] anyVector, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            var request = new VectorSearchRequest { TopK = 50, MinScore = -1, IncludeUnpublished = true };
            var count = (await vectorSearch.SearchAsync(anyVector, request)).Count;
            if (count == expected || DateTime.UtcNow >= deadline)
            {
                return count;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }

    private static string Format(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Titles(IReadOnlyList<VectorSearchResult> results) =>
        results.Count == 0 ? "no results" : string.Join(", ", results.Select(r => r.ArticleTitle).Distinct());

    private static string Start(string text) => string.Join(' ', text.Split(' ', 6).Take(5)) + " ...";

    private static string FirstWords(string text, int count) => string.Join(' ', text.Split(' ').Take(count));
}
