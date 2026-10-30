namespace ContosoSupportCopilot.Domain.Models;

public record EmbeddingOptions
{
    // Null keeps the model's default vector length. Models such as text-embedding-3-small
    // can return shorter vectors; every vector in one index must have the same length.
    public int? Dimensions { get; init; }
}

public record EmbeddingResult
{
    public float[] Vector { get; init; } = Array.Empty<float>();
    public int TokenCount { get; init; }
    public string ModelId { get; init; } = string.Empty;
}
