namespace ContosoSupportCopilot.Domain.Models;

public record KnowledgeArticle
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string ProductArea { get; init; } = string.Empty;
    public string[] Tags { get; init; } = Array.Empty<string>();
    public DateTime LastUpdated { get; init; }
    public bool IsPublished { get; init; }
    public List<ArticleChunk> Chunks { get; init; } = new();
}

public record ArticleChunk
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public float[] Embedding { get; init; } = Array.Empty<float>();
    public int ChunkIndex { get; init; }
}
