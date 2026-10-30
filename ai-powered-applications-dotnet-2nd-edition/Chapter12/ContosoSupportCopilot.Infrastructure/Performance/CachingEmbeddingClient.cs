using System.Security.Cryptography;
using System.Text;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Telemetry;
using Microsoft.Extensions.Caching.Memory;

namespace ContosoSupportCopilot.Infrastructure.Performance;

// The decorator from Chapter 7's activity: the same text, model and vector length always give the
// same vector, so a repeated request is answered from memory. It owns its cache, because a size
// limit applies to every entry of a cache and the shared IMemoryCache holds entries without a size.
public sealed class CachingEmbeddingClient : IEmbeddingClient, IDisposable
{
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(30);

    private readonly IEmbeddingClient _inner;
    private readonly ITelemetryCollector _telemetry;
    private readonly string _modelId;
    private readonly MemoryCache _cache;

    public CachingEmbeddingClient(IEmbeddingClient inner, ITelemetryCollector telemetry, string modelId, int maxEntries)
    {
        _inner = inner;
        _telemetry = telemetry;
        _modelId = modelId;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = maxEntries });
    }

    public async Task<EmbeddingResult> GenerateAsync(string text, EmbeddingOptions? options = null, CancellationToken cancellationToken = default)
    {
        var key = CreateKey(text, options?.Dimensions);
        if (_cache.TryGetValue(key, out EmbeddingResult? cached) && cached is not null)
        {
            // A hit spends no tokens, so cost tracking must not count them again.
            _telemetry.TrackEmbeddingCache(hit: true, tokensSaved: cached.TokenCount);
            return cached with { TokenCount = 0 };
        }

        var result = await _inner.GenerateAsync(text, options, cancellationToken);
        _cache.Set(key, result, new MemoryCacheEntryOptions { Size = 1, SlidingExpiration = SlidingExpiration });
        _telemetry.TrackEmbeddingCache(hit: false, tokensSaved: 0);
        return result;
    }

    // The key holds a hash of the text, never the text itself, which can be long or sensitive.
    private string CreateKey(string text, int? dimensions)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        return $"embedding:{_modelId}:{dimensions?.ToString() ?? "default"}:{hash}";
    }

    public void Dispose() => _cache.Dispose();
}
