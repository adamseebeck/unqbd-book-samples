using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Infrastructure.Search;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class VectorSearchServiceTests
{
    private static readonly KnowledgeArticle Published = new() { Id = Guid.NewGuid(), Title = "Published", ProductArea = "Payments", IsPublished = true };
    private static readonly KnowledgeArticle Draft = new() { Id = Guid.NewGuid(), Title = "Draft", ProductArea = "Payments", IsPublished = false };
    private static readonly KnowledgeArticle OtherArea = new() { Id = Guid.NewGuid(), Title = "Other", ProductArea = "API", IsPublished = true };

    [Fact]
    public async Task SearchAsync_FiltersByProductAreaAndPublication_AndSortsByScore()
    {
        var store = await CreateStoreAsync();

        var results = await store.SearchAsync([1f, 0f], new VectorSearchRequest { TopK = 5, ProductAreaFilter = "Payments" });

        Assert.Equal(["Published", "Published"], results.Select(r => r.ArticleTitle));
        Assert.True(results[0].Score > results[1].Score);
        Assert.All(results, r => Assert.Empty(r.Chunk.Embedding));
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData("", 3)]
    [InlineData("payments", 0)]
    public async Task SearchAsync_TreatsAnEmptyFilterAsNoFilter_AndMatchesAreasExactly(string? productArea, int expected)
    {
        var store = await CreateStoreAsync();

        var results = await store.SearchAsync([1f, 0f], new VectorSearchRequest { TopK = 10, ProductAreaFilter = productArea });

        Assert.Equal(expected, results.Count);
    }

    [Fact]
    public async Task SearchAsync_IncludesDrafts_OnlyWhenAsked()
    {
        var store = await CreateStoreAsync();

        var results = await store.SearchAsync([1f, 0f], new VectorSearchRequest { TopK = 5, ProductAreaFilter = "Payments", IncludeUnpublished = true });

        Assert.Contains(results, r => r.ArticleId == Draft.Id);
    }

    [Fact]
    public async Task SearchAsync_DropsResultsBelowMinScore_AndReturnsAtMostTopK()
    {
        var store = await CreateStoreAsync();

        var strict = await store.SearchAsync([1f, 0f], new VectorSearchRequest { TopK = 5, MinScore = 0.99 });
        var top1 = await store.SearchAsync([1f, 0f], new VectorSearchRequest { TopK = 1 });

        Assert.Equal(2, strict.Count);
        Assert.Single(top1);
    }

    [Fact]
    public async Task UpsertArticleChunksAsync_ReplacesTheArticlesChunks()
    {
        var store = await CreateStoreAsync();

        await store.UpsertArticleChunksAsync(Published, [Chunk(0, [0f, 1f])]);
        var results = await store.SearchAsync([0f, 1f], new VectorSearchRequest { TopK = 10, ProductAreaFilter = "Payments" });

        Assert.Single(results);
        Assert.Equal(1.0, results[0].Score, precision: 5);
    }

    [Fact]
    public async Task SearchAsync_Throws_WhenTheQueryVectorHasADifferentLength()
    {
        var store = await CreateStoreAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => store.SearchAsync([1f, 0f, 0f], new VectorSearchRequest()));
    }

    [Theory]
    [InlineData("Payments", false, "isPublished eq true and productArea eq 'Payments'")]
    [InlineData("O'Brien Payments", true, "productArea eq 'O''Brien Payments'")]
    [InlineData(null, true, null)]
    public void BuildFilter_QuotesAndEscapesTheProductArea(string? productArea, bool includeUnpublished, string? expected)
    {
        var filter = AzureAISearchVectorService.BuildFilter(new VectorSearchRequest { ProductAreaFilter = productArea, IncludeUnpublished = includeUnpublished });

        Assert.Equal(expected, filter);
    }

    private static async Task<InMemoryVectorSearchService> CreateStoreAsync()
    {
        var store = new InMemoryVectorSearchService();
        await store.UpsertArticleChunksAsync(Published, [Chunk(0, [1f, 0f]), Chunk(1, [0.8f, 0.6f])]);
        await store.UpsertArticleChunksAsync(Draft, [Chunk(0, [1f, 0.1f])]);
        await store.UpsertArticleChunksAsync(OtherArea, [Chunk(0, [1f, 0f])]);
        return store;
    }

    private static ArticleChunk Chunk(int index, float[] embedding) =>
        new() { Id = Guid.NewGuid(), ChunkIndex = index, Content = $"chunk {index}", Embedding = embedding };
}
