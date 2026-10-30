using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Resilience;
using ContosoSupportCopilot.Infrastructure.Search;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class VectorKnowledgeRepositoryTests
{
    private readonly Mock<ISupportContext> _inner = new();
    private readonly Mock<IEmbeddingClient> _embeddings = new();
    private readonly Mock<IVectorSearchService> _vectorSearch = new();
    private readonly VectorStoreOptions _options = new() { TopK = 4, MinScore = 0.25 };

    private readonly KnowledgeArticle _lockouts = new() { Id = Guid.NewGuid(), Title = "Lockouts", ProductArea = "Authentication", IsPublished = true };
    private readonly KnowledgeArticle _passwords = new() { Id = Guid.NewGuid(), Title = "Passwords", ProductArea = "Authentication", IsPublished = true };

    public VectorKnowledgeRepositoryTests()
    {
        _embeddings
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<EmbeddingOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddingResult { Vector = [1f, 0f], TokenCount = 3, ModelId = "test-embedding" });
    }

    [Fact]
    public async Task SearchKnowledgeBaseAsync_ReturnsDatabaseArticles_WithOnlyTheirMatchingChunks()
    {
        VectorSearchRequest? sent = null;
        _vectorSearch
            .Setup(v => v.SearchAsync(It.IsAny<float[]>(), It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback((float[] _, VectorSearchRequest request, CancellationToken _) => sent = request)
            .ReturnsAsync([Result(_passwords, 2, 0.9), Result(_lockouts, 0, 0.8), Result(_passwords, 0, 0.7)]);
        _inner
            .Setup(c => c.GetArticlesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_lockouts, _passwords]);

        var articles = await CreateRepository().SearchKnowledgeBaseAsync("locked out", "Authentication");

        Assert.Equal(["Passwords", "Lockouts"], articles.Select(a => a.Title));
        Assert.Equal([2, 0], articles[0].Chunks.Select(c => c.ChunkIndex));
        Assert.Equal(new VectorSearchRequest { TopK = 4, MinScore = 0.25, ProductAreaFilter = "Authentication" }, sent);
    }

    [Fact]
    public async Task SearchKnowledgeBaseAsync_LeavesOutArticles_UnpublishedSinceIndexing()
    {
        _vectorSearch
            .Setup(v => v.SearchAsync(It.IsAny<float[]>(), It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Result(_lockouts, 0, 0.8)]);
        _inner
            .Setup(c => c.GetArticlesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_lockouts with { IsPublished = false }]);

        var articles = await CreateRepository().SearchKnowledgeBaseAsync("locked out", "Authentication");

        Assert.Empty(articles);
    }

    [Fact]
    public async Task IndexArticleAsync_EmbedsTheTitleWithEachChunk_AndStoresTheVectors()
    {
        var article = new KnowledgeArticle { Id = Guid.NewGuid(), Title = "Lockouts", Content = "Accounts unlock after 30 minutes." };
        IReadOnlyList<ArticleChunk>? stored = null;
        _vectorSearch
            .Setup(v => v.UpsertArticleChunksAsync(article, It.IsAny<IReadOnlyList<ArticleChunk>>(), It.IsAny<CancellationToken>()))
            .Callback((KnowledgeArticle _, IReadOnlyList<ArticleChunk> chunks, CancellationToken _) => stored = chunks)
            .Returns(Task.CompletedTask);

        var count = await CreateRepository().IndexArticleAsync(article);

        Assert.Equal(1, count);
        _embeddings.Verify(e => e.GenerateAsync("Lockouts\nAccounts unlock after 30 minutes.", null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(new[] { 1f, 0f }, Assert.Single(stored!).Embedding);
    }

    [Fact]
    public async Task SupportEmbeddingClient_UsesTheConfiguredDimensions_UnlessTheCallerSetsThem()
    {
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(KernelResiliencePipeline.Name, (builder, _) => builder.AddKernelResilience(new KernelResilienceOptions()));
        var client = new SupportEmbeddingClient(
            new OfflineEmbeddingGenerator(),
            registry,
            Options.Create(new VectorStoreOptions { Dimensions = 256 }));

        var configured = await client.GenerateAsync("Reset links expire after one hour.");
        var requested = await client.GenerateAsync("Reset links expire after one hour.", new EmbeddingOptions { Dimensions = 64 });

        Assert.Equal(256, configured.Vector.Length);
        Assert.Equal(64, requested.Vector.Length);
        Assert.Equal(OfflineEmbeddingGenerator.ModelName, configured.ModelId);
        Assert.True(configured.TokenCount > 0);
    }

    [Fact]
    public async Task SupportEmbeddingClient_ReportsATimeoutAndAnOpenCircuit_WithoutPollyTypes()
    {
        // A generator that never answers, behind a short timeout and a circuit that the test opens by hand.
        var generator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        generator
            .Setup(g => g.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(async (IEnumerable<string> _, EmbeddingGenerationOptions? _, CancellationToken token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new GeneratedEmbeddings<Embedding<float>>();
            });
        var circuit = new CircuitBreakerManualControl();
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(KernelResiliencePipeline.Name, (builder, _) => builder
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions { ManualControl = circuit })
            .AddTimeout(TimeSpan.FromMilliseconds(50)));
        var client = new SupportEmbeddingClient(generator.Object, registry, Options.Create(new VectorStoreOptions()));

        var timeout = await Assert.ThrowsAsync<TimeoutException>(() => client.GenerateAsync("Reset links expire after one hour."));
        await circuit.IsolateAsync();
        var open = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GenerateAsync("Reset links expire after one hour."));

        Assert.IsType<TimeoutRejectedException>(timeout.InnerException);
        Assert.IsAssignableFrom<BrokenCircuitException>(open.InnerException);
    }

    private VectorKnowledgeRepository CreateRepository() => new(
        _inner.Object,
        _embeddings.Object,
        _vectorSearch.Object,
        new ArticleChunkingService(),
        Options.Create(_options),
        NullLogger<VectorKnowledgeRepository>.Instance);

    private static VectorSearchResult Result(KnowledgeArticle article, int chunkIndex, double score) => new()
    {
        Chunk = new ArticleChunk { Id = Guid.NewGuid(), ChunkIndex = chunkIndex, Content = $"{article.Title} chunk {chunkIndex}" },
        Score = score,
        ArticleId = article.Id,
        ArticleTitle = article.Title,
        ProductArea = article.ProductArea
    };
}
