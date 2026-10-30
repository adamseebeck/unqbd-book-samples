namespace ContosoSupportCopilot.Domain.Models;

public record VectorSearchRequest
{
    public int TopK { get; init; } = 5;

    // Cosine similarity floor from -1 to 1; 0 keeps every match pointing the same general way.
    // The application reads its default from VectorStore:MinScore.
    public double MinScore { get; init; }

    public string? ProductAreaFilter { get; init; }
    public bool IncludeUnpublished { get; init; }
}

public record VectorSearchResult
{
    public ArticleChunk Chunk { get; init; } = null!;

    // Cosine similarity between the query and the chunk, from every vector store.
    public double Score { get; init; }

    public Guid ArticleId { get; init; }
    public string ArticleTitle { get; init; } = string.Empty;
    public string ProductArea { get; init; } = string.Empty;
}
