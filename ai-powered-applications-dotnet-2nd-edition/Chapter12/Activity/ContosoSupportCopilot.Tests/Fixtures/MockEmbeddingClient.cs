using System.Security.Cryptography;
using System.Text;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Tests.Fixtures;

// The same text always gets the same vector, in every run and on every machine: the seed comes
// from SHA-256 of the text, not from string.GetHashCode, which changes with every process.
public sealed class MockEmbeddingClient : IEmbeddingClient
{
    public const int DefaultDimensions = 384;

    public int Calls { get; private set; }

    public Task<EmbeddingResult> GenerateAsync(string text, EmbeddingOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        var dimensions = options?.Dimensions ?? DefaultDimensions;
        var seed = BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim().ToLowerInvariant())), 0);
        var random = new Random(seed);

        var vector = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            vector[i] = (float)(random.NextDouble() * 2 - 1);
        }

        return Task.FromResult(new EmbeddingResult { Vector = vector, TokenCount = Math.Max(1, text.Length / 4), ModelId = "mock-embedding" });
    }
}
