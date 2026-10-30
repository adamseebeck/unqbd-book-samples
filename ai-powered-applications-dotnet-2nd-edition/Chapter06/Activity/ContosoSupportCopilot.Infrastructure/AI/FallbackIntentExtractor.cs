using System.Security.Cryptography;
using System.Text;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class FallbackIntentExtractor : IIntentExtractor
{
    private readonly IIntentExtractor _innerExtractor;
    private readonly ILogger<FallbackIntentExtractor> _logger;
    private readonly IMemoryCache _cache;
    private readonly FallbackOptions _options;

    public FallbackIntentExtractor(
        IIntentExtractor innerExtractor,
        ILogger<FallbackIntentExtractor> logger,
        IMemoryCache cache,
        IOptions<FallbackOptions> options)
    {
        _innerExtractor = innerExtractor;
        _logger = logger;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<SupportIntent> ExtractAsync(string ticketDescription, CancellationToken cancellationToken = default)
    {
        // A hash keeps customer text out of the cache keys and gives every key the same small size.
        var cacheKey = $"intent:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ticketDescription)))}";

        try
        {
            var intent = await _innerExtractor.ExtractAsync(ticketDescription, cancellationToken);
            if (_options.EnableCache)
            {
                // Size counts toward MemoryCacheOptions.SizeLimit when the application sets one.
                _cache.Set(cacheKey, intent, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = _options.CacheDuration, Size = 1 });
            }

            return intent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Nothing cached: the failure reaches the workflow, whose own fallback keeps the ticket's
            // priority and product area and records the error on the turn's run.
            if (!_options.EnableCache || !_cache.TryGetValue(cacheKey, out SupportIntent? cached) || cached is null)
            {
                throw;
            }

            _logger.LogWarning(ex, "Intent extraction failed; using the cached intent for the same description");
            return cached;
        }
    }
}
