using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Search;

public sealed class AzureAISearchVectorService : IVectorSearchService
{
    private const string VectorProfile = "embedding-profile";

    // By default the SDK throws only when the whole request fails, so a rejected document would be lost silently.
    private static readonly IndexDocumentsOptions FailOnAnyDocument = new() { ThrowOnAnyError = true };

    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly string _indexName;
    private readonly int _dimensions;
    private readonly string _embeddingModel;
    private volatile bool _indexReady;

    public AzureAISearchVectorService(
        SearchIndexClient indexClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<AzureAISearchOptions> searchOptions,
        IOptions<VectorStoreOptions> vectorOptions)
    {
        _indexClient = indexClient;
        _indexName = searchOptions.Value.IndexName;
        _searchClient = indexClient.GetSearchClient(_indexName);
        _dimensions = vectorOptions.Value.Dimensions
            ?? throw new InvalidOperationException("VectorStore:Dimensions is required for Azure AI Search.");

        // The model behind AI:EmbeddingDeployment. The index keeps it with every chunk, because vectors from
        // two models are not comparable, even when they have the same length.
        _embeddingModel = embeddingGenerator.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId
            ?? throw new InvalidOperationException("The embedding generator does not name its model.");
    }

    public async Task UpsertArticleChunksAsync(
        KnowledgeArticle article,
        IReadOnlyList<ArticleChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        await EnsureIndexAsync(cancellationToken);

        var documents = chunks.Select(chunk => new SearchDocument
        {
            ["id"] = chunk.Id.ToString(),
            ["articleId"] = article.Id.ToString(),
            ["articleTitle"] = article.Title,
            ["productArea"] = article.ProductArea,
            ["isPublished"] = article.IsPublished,
            ["chunkIndex"] = chunk.ChunkIndex,
            ["content"] = chunk.Content,
            ["embedding"] = chunk.Embedding,
            ["embeddingModel"] = _embeddingModel
        }).ToList();

        if (documents.Count > 0)
        {
            await _searchClient.MergeOrUploadDocumentsAsync(documents, FailOnAnyDocument, cancellationToken);
        }

        // Delete the chunks that an earlier, longer version of the article left in the index.
        var keep = chunks.Select(chunk => chunk.Id.ToString()).ToHashSet();
        var existing = await _searchClient.SearchAsync<SearchDocument>(
            new SearchOptions { Filter = SearchFilter.Create($"articleId eq {article.Id.ToString()}"), Select = { "id" }, Size = 1000 },
            cancellationToken);

        var stale = new List<string>();
        await foreach (var result in existing.Value.GetResultsAsync())
        {
            var id = result.Document.GetString("id");
            if (!keep.Contains(id))
            {
                stale.Add(id);
            }
        }

        if (stale.Count > 0)
        {
            await _searchClient.DeleteDocumentsAsync("id", stale, FailOnAnyDocument, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        VectorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        // A search before the first upload finds an empty index instead of a missing one.
        await EnsureIndexAsync(cancellationToken);

        var options = new SearchOptions
        {
            VectorSearch = new VectorSearchOptions
            {
                Queries = { new VectorizedQuery(queryVector) { KNearestNeighborsCount = request.TopK, Fields = { "embedding" } } },
                // Filter first, so the nearest neighbors come from the matching chunks only.
                FilterMode = VectorFilterMode.PreFilter
            },
            Filter = BuildFilter(request),
            Size = request.TopK,
            Select = { "id", "articleId", "articleTitle", "productArea", "chunkIndex", "content" }
        };

        // No search text: a pure vector query, so each score comes from the vector distance alone.
        var response = await _searchClient.SearchAsync<SearchDocument>(options, cancellationToken);

        var results = new List<VectorSearchResult>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            if (result.Score is not double score)
            {
                continue;
            }

            // For the cosine metric, @search.score is 1 / (1 + cosine distance). Converting it back to the
            // cosine similarity gives MinScore the same meaning as in the in-memory store.
            var similarity = 2 - (1 / score);
            if (similarity < request.MinScore)
            {
                continue;
            }

            var document = result.Document;
            results.Add(new VectorSearchResult
            {
                Chunk = new ArticleChunk
                {
                    Id = Guid.Parse(document.GetString("id")),
                    Content = document.GetString("content"),
                    ChunkIndex = document.GetInt32("chunkIndex") ?? 0
                },
                Score = similarity,
                ArticleId = Guid.Parse(document.GetString("articleId")),
                ArticleTitle = document.GetString("articleTitle"),
                ProductArea = document.GetString("productArea")
            });
        }

        return results;
    }

    // Public, so a unit test can check the escaping without a search service.
    public static string? BuildFilter(VectorSearchRequest request)
    {
        var filters = new List<string>();
        if (!request.IncludeUnpublished)
        {
            filters.Add("isPublished eq true");
        }

        if (!string.IsNullOrEmpty(request.ProductAreaFilter))
        {
            // SearchFilter.Create quotes and escapes the value, so text from a request cannot change the filter.
            filters.Add(SearchFilter.Create($"productArea eq {request.ProductAreaFilter}"));
        }

        return filters.Count > 0 ? string.Join(" and ", filters) : null;
    }

    // The index schema lives in code: field names, filterable fields and the vector configuration.
    private async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        if (_indexReady)
        {
            return;
        }

        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SimpleField("articleId", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("articleTitle", SearchFieldDataType.String),
                new SimpleField("productArea", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("isPublished", SearchFieldDataType.Boolean) { IsFilterable = true },
                new SimpleField("chunkIndex", SearchFieldDataType.Int32),
                new SearchableField("content"),
                new VectorSearchField("embedding", _dimensions, VectorProfile),
                new SimpleField("embeddingModel", SearchFieldDataType.String) { IsFilterable = true }
            },
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile(VectorProfile, "hnsw") },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw")
                    {
                        Parameters = new HnswParameters { Metric = VectorSearchAlgorithmMetric.Cosine }
                    }
                }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);

        // A query embedded by one model finds wrong neighbors among another model's vectors, without any error.
        // So an index that holds a chunk from another model, or from before the model was recorded, is refused.
        var otherModel = await _searchClient.SearchAsync<SearchDocument>(
            new SearchOptions { Filter = SearchFilter.Create($"embeddingModel ne {_embeddingModel}"), Select = { "embeddingModel" }, Size = 1 },
            cancellationToken);
        await foreach (var result in otherModel.Value.GetResultsAsync())
        {
            throw new InvalidOperationException(
                $"Index '{_indexName}' holds vectors from the embedding model '{result.Document.GetString("embeddingModel") ?? "(not recorded)"}', " +
                $"not from '{_embeddingModel}'. Embed every article again into a new index (Search:IndexName) before you switch models.");
        }

        _indexReady = true;
    }
}
