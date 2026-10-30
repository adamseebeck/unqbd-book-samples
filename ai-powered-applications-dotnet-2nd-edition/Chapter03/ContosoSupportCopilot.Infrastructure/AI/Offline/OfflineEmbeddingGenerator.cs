using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>
/// An <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that hashes words and word
/// pairs into a fixed-length vector. The same text always produces the same vector, and
/// texts that share words point in similar directions, so offline vector search returns
/// sensible neighbors. The vectors are not comparable with a real embedding model's vectors.
/// </summary>
public sealed partial class OfflineEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public const int DefaultDimensions = 384;
    public const string ModelName = "offline-hash-embedding";

    private readonly int _dimensions;
    private readonly EmbeddingGeneratorMetadata _metadata;

    public OfflineEmbeddingGenerator(int dimensions = DefaultDimensions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dimensions, 8);
        _dimensions = dimensions;
        _metadata = new EmbeddingGeneratorMetadata(OfflineChatClient.ProviderName, defaultModelId: ModelName, defaultModelDimensions: dimensions);
    }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        var dimensions = options?.Dimensions ?? _dimensions;
        var embeddings = new GeneratedEmbeddings<Embedding<float>>();
        long tokens = 0;

        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            embeddings.Add(new Embedding<float>(CreateVector(value, dimensions)) { ModelId = ModelName });
            tokens += OfflineResponder.EstimateTokens(value);
        }

        embeddings.Usage = new UsageDetails { InputTokenCount = tokens, TotalTokenCount = tokens };
        return Task.FromResult(embeddings);
    }

    public static float[] CreateVector(string text, int dimensions)
    {
        var vector = new float[dimensions];
        var words = Word().Matches(text.ToLowerInvariant()).Select(m => m.Value).ToArray();

        for (var i = 0; i < words.Length; i++)
        {
            AddFeature(vector, words[i], 1.0f);
            if (i + 1 < words.Length)
            {
                AddFeature(vector, $"{words[i]} {words[i + 1]}", 0.5f);
            }
        }

        var length = MathF.Sqrt(vector.Sum(v => v * v));
        if (length == 0)
        {
            vector[0] = 1;
            return vector;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= length;
        }

        return vector;
    }

    private static void AddFeature(float[] vector, string feature, float weight)
    {
        // FNV-1a gives a stable hash on every run, unlike string.GetHashCode.
        var hash = 14695981039346656037UL;
        foreach (var b in Encoding.UTF8.GetBytes(feature))
        {
            hash = (hash ^ b) * 1099511628211UL;
        }

        var index = (int)(hash % (ulong)vector.Length);
        vector[index] += (hash >> 63) == 0 ? weight : -weight;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return _metadata;
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Word();
}
