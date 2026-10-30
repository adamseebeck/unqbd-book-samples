using System.Runtime.CompilerServices;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class FallbackDraftingServiceTests
{
    private readonly FallbackOptions _options = new();
    private readonly SupportTicket _ticket = new() { Id = Guid.NewGuid(), Subject = "API returns 429" };
    private readonly Mock<IDraftingService> _inner = new();

    [Fact]
    public async Task DraftReplyAsync_ReturnsTheCachedDraft_WhenTheServiceFails()
    {
        var service = CreateService();
        _inner
            .SetupSequence(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftReply { Text = "Wait for the Retry-After interval.", ModelId = "test-model", InputTokens = 90, OutputTokens = 30 })
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        await service.DraftReplyAsync(_ticket, new DraftingOptions());
        var reply = await service.DraftReplyAsync(_ticket, new DraftingOptions());

        Assert.Equal("Wait for the Retry-After interval.", reply.Text);
        Assert.True(reply.IsFallback);
        Assert.Equal(0, reply.InputTokens);
    }

    [Fact]
    public async Task DraftReplyAsync_ReturnsTheFallbackMessage_WhenNothingIsCached()
    {
        _inner
            .Setup(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var reply = await CreateService().DraftReplyAsync(_ticket, new DraftingOptions());

        Assert.Equal(_options.FallbackMessage, reply.Text);
        Assert.True(reply.IsFallback);
    }

    [Fact]
    public async Task DraftReplyAsync_LetsCancellationThrough()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _inner
            .Setup(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService().DraftReplyAsync(_ticket, new DraftingOptions(), cancellation.Token));
    }

    [Fact]
    public async Task DraftReplyAsync_KeepsTheModelsDraft_WhenTheCacheHasASizeLimit()
    {
        var service = new FallbackDraftingService(
            _inner.Object,
            NullLogger<FallbackDraftingService>.Instance,
            new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 }),
            Options.Create(_options));
        _inner
            .Setup(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftReply { Text = "Wait for the Retry-After interval.", ModelId = "test-model" });

        var reply = await service.DraftReplyAsync(_ticket, new DraftingOptions());

        Assert.Equal("Wait for the Retry-After interval.", reply.Text);
        Assert.False(reply.IsFallback);
    }

    [Fact]
    public async Task DraftReplyStreamingAsync_KeepsPartialText_AndAddsTheFallback_WhenTheStreamFails()
    {
        _inner
            .Setup(d => d.DraftReplyStreamingAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .Returns((SupportTicket _, DraftingOptions _, CancellationToken token) => FailAfter(["Thank you ", "for waiting. "], token));

        var chunks = new List<string>();
        await foreach (var chunk in CreateService().DraftReplyStreamingAsync(_ticket, new DraftingOptions()))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Thank you for waiting. ", chunks[0] + chunks[1]);
        Assert.Equal(Environment.NewLine + Environment.NewLine + _options.FallbackMessage, chunks[2]);
    }

    [Fact]
    public async Task DraftReplyStreamingAsync_YieldsOnlyTheFallback_WhenTheStreamFailsImmediately()
    {
        _inner
            .Setup(d => d.DraftReplyStreamingAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .Returns((SupportTicket _, DraftingOptions _, CancellationToken token) => FailAfter([], token));

        var chunks = new List<string>();
        await foreach (var chunk in CreateService().DraftReplyStreamingAsync(_ticket, new DraftingOptions()))
        {
            chunks.Add(chunk);
        }

        Assert.Equal([_options.FallbackMessage], chunks);
    }

    private FallbackDraftingService CreateService() => new(
        _inner.Object,
        NullLogger<FallbackDraftingService>.Instance,
        new MemoryCache(new MemoryCacheOptions()),
        Options.Create(_options));

    private static async IAsyncEnumerable<string> FailAfter(string[] chunks, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }

        throw new HttpRequestException("The connection closed during the stream.");
    }
}
