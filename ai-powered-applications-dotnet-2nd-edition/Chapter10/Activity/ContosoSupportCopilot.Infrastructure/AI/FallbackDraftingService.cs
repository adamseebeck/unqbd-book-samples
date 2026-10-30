using System.Runtime.CompilerServices;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class FallbackDraftingService : IDraftingService
{
    private readonly IDraftingService _innerService;
    private readonly ILogger<FallbackDraftingService> _logger;
    private readonly IMemoryCache _cache;
    private readonly FallbackOptions _options;

    public FallbackDraftingService(
        IDraftingService innerService,
        ILogger<FallbackDraftingService> logger,
        IMemoryCache cache,
        IOptions<FallbackOptions> options)
    {
        _innerService = innerService;
        _logger = logger;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<DraftReply> DraftReplyAsync(SupportTicket ticket, DraftingOptions options, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(ticket);
        DraftReply reply;

        try
        {
            reply = await _innerService.DraftReplyAsync(ticket, options, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Drafting failed for ticket {TicketId}; returning a fallback reply", ticket.Id);

            // A recent draft for the same conversation beats the generic message.
            if (_options.EnableCache && _cache.TryGetValue(cacheKey, out DraftReply? cached) && cached is not null)
            {
                return cached with { IsFallback = true, InputTokens = 0, OutputTokens = 0 };
            }

            return new DraftReply { Text = _options.FallbackMessage, IsFallback = true };
        }

        // Outside the try, so a cache problem can never turn the model's draft into the fallback. Size counts
        // toward MemoryCacheOptions.SizeLimit when the application sets one.
        if (_options.EnableCache)
        {
            _cache.Set(cacheKey, reply, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = _options.CacheDuration, Size = 1 });
        }

        return reply;
    }

    public async IAsyncEnumerable<string> DraftReplyStreamingAsync(
        SupportTicket ticket,
        DraftingOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // C# does not allow yield return inside a try block that has a catch clause, so only
        // MoveNextAsync runs inside try/catch, and each chunk is yielded after it.
        var enumerator = _innerService.DraftReplyStreamingAsync(ticket, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        var receivedText = false;
        var failed = false;

        try
        {
            while (true)
            {
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Streaming draft failed for ticket {TicketId}", ticket.Id);
                    failed = true;
                    break;
                }

                receivedText = true;
                yield return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        if (failed)
        {
            // Text that already reached the caller stays; the fallback message follows on its own line.
            yield return receivedText
                ? Environment.NewLine + Environment.NewLine + _options.FallbackMessage
                : _options.FallbackMessage;
        }
    }

    // The message count is part of the key, so a cached draft is reused only for an unchanged conversation.
    private static string CacheKey(SupportTicket ticket) => $"draft:{ticket.Id}:{ticket.Messages.Count}";
}
