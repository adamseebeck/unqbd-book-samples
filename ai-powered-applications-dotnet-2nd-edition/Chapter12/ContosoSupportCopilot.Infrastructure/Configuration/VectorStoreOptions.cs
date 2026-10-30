using System.ComponentModel.DataAnnotations;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

public enum VectorStoreProvider
{
    InMemory,
    AzureAISearch
}

public class VectorStoreOptions : IValidatableObject
{
    public const string SectionName = "VectorStore";

    public VectorStoreProvider Provider { get; set; } = VectorStoreProvider.InMemory;

    // One vector length for stored and query vectors. Null keeps the embedding model's default.
    [Range(8, 4096, ErrorMessage = "VectorStore:Dimensions must be between 8 and 4096")]
    public int? Dimensions { get; set; }

    [Range(1, 50, ErrorMessage = "VectorStore:TopK must be between 1 and 50")]
    public int TopK { get; set; } = 5;

    // A cosine similarity floor. Every embedding model has its own range, so calibrate it on your data.
    [Range(-1.0, 1.0, ErrorMessage = "VectorStore:MinScore must be between -1 and 1")]
    public double MinScore { get; set; } = 0.2;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Provider == VectorStoreProvider.AzureAISearch && Dimensions is null)
        {
            yield return new ValidationResult(
                "Set VectorStore:Dimensions for Azure AI Search: an index stores vectors of one fixed length",
                [nameof(Dimensions)]);
        }
    }
}
