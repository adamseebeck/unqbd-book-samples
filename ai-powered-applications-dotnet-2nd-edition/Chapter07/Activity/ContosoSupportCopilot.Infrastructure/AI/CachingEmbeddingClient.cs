using System.Security.Cryptography;
using System.Text;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.Extensions.Caching.Memory;

namespace ContosoSupportCopilot.Infrastructure.AI;

// A decorator: the same text, model and vector length always produce the same embedding,
// so a repeated request is answered from memory instead of the embedding model.
public sealed class CachingEmbeddingClient : IEmbeddingClient
{
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(5);

    private readonly IEmbeddingClient _inner;
    private readonly IMemoryCache _cache;
    private readonly string _modelId;

    public CachingEmbeddingClient(IEmbeddingClient inner, IMemoryCache cache, string modelId)
    {
        _inner = inner;
        _cache = cache;
        _modelId = modelId;
    }

    public async Task<EmbeddingResult> GenerateAsync(
        string text,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var key = CreateKey(text, options?.Dimensions);
        if (_cache.TryGetValue(key, out EmbeddingResult? cached) && cached is not null)
        {
            // A cache hit spends no tokens, so cost tracking must not count them again.
            return cached with { TokenCount = 0 };
        }

        var result = await _inner.GenerateAsync(text, options, cancellationToken);
        _cache.Set(key, result, new MemoryCacheEntryOptions { SlidingExpiration = SlidingExpiration });
        return result;
    }

    // The key holds a hash of the text, never the text itself, which can be long or sensitive.
    private string CreateKey(string text, int? dimensions)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        return $"embedding:{_modelId}:{dimensions?.ToString() ?? "default"}:{hash}";
    }
}
