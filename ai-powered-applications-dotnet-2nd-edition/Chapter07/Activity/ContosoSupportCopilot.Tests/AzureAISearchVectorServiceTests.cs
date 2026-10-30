using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Search;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// The Azure AI Search store without a search service: the SDK clients are mocked, so the tests see exactly
// what the service sends and what it does with the answers.
public class AzureAISearchVectorServiceTests
{
    private readonly Mock<SearchIndexClient> _indexClient = new();
    private readonly Mock<SearchClient> _searchClient = new();
    private readonly List<SearchOptions> _queries = [];
    private List<SearchDocument> _indexed = [];

    public AzureAISearchVectorServiceTests()
    {
        _indexClient.Setup(c => c.GetSearchClient(It.IsAny<string>())).Returns(_searchClient.Object);
        _indexClient
            .Setup(c => c.CreateOrUpdateIndexAsync(It.IsAny<SearchIndex>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SearchIndex index, bool _, bool _, CancellationToken _) => Response.FromValue(index, Mock.Of<Response>()));
        _searchClient
            .Setup(c => c.MergeOrUploadDocumentsAsync(It.IsAny<IEnumerable<SearchDocument>>(), It.IsAny<IndexDocumentsOptions>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<SearchDocument> documents, IndexDocumentsOptions _, CancellationToken _) => _indexed = documents.ToList())
            .ReturnsAsync(Response.FromValue(SearchModelFactory.IndexDocumentsResult([]), Mock.Of<Response>()));
        StoredChunks();
    }

    [Fact]
    public async Task UpsertArticleChunksAsync_RecordsTheEmbeddingModel_WithEveryChunk()
    {
        var article = new KnowledgeArticle { Id = Guid.NewGuid(), Title = "Lockouts", ProductArea = "Authentication", IsPublished = true };

        await CreateService().UpsertArticleChunksAsync(article, [new ArticleChunk { Id = Guid.NewGuid(), Content = "Accounts unlock.", Embedding = [1f, 0f] }]);

        Assert.Equal(OfflineEmbeddingGenerator.ModelName, Assert.Single(_indexed)["embeddingModel"]);
        Assert.Contains(_queries, q => q.Filter == $"embeddingModel ne '{OfflineEmbeddingGenerator.ModelName}'");
    }

    [Theory]
    [InlineData("text-embedding-ada-002")]
    [InlineData(null)]
    public async Task SearchAsync_RefusesAnIndex_WithChunksFromAnotherEmbeddingModel(string? storedModel)
    {
        StoredChunks(new SearchDocument { ["embeddingModel"] = storedModel });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().SearchAsync([1f, 0f], new VectorSearchRequest()));

        Assert.Contains(storedModel ?? "(not recorded)", error.Message);
        Assert.Contains(OfflineEmbeddingGenerator.ModelName, error.Message);
    }

    private AzureAISearchVectorService CreateService() => new(
        _indexClient.Object,
        new OfflineEmbeddingGenerator(),
        Options.Create(new AzureAISearchOptions { Endpoint = "https://search.example", ApiKey = "test" }),
        Options.Create(new VectorStoreOptions { Provider = VectorStoreProvider.AzureAISearch, Dimensions = 2 }));

    // Every query returns these documents; the default is an index without chunks from another model.
    private void StoredChunks(params SearchDocument[] documents) =>
        _searchClient
            .Setup(c => c.SearchAsync<SearchDocument>(It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .Callback((SearchOptions options, CancellationToken _) => _queries.Add(options))
            .ReturnsAsync(() => Response.FromValue(
                SearchModelFactory.SearchResults(documents.Select(d => SearchModelFactory.SearchResult(d, 1.0, null)).ToList(), null, null, null, Mock.Of<Response>()),
                Mock.Of<Response>()));
}
