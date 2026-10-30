using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class CachingEmbeddingClientTests
{
    private readonly Mock<IEmbeddingClient> _inner = new();

    public CachingEmbeddingClientTests()
    {
        _inner
            .Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<EmbeddingOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, EmbeddingOptions? options, CancellationToken _) => new EmbeddingResult
            {
                Vector = [text.Length, options?.Dimensions ?? 0],
                TokenCount = 8,
                ModelId = "test-embedding"
            });
    }

    [Fact]
    public async Task GenerateAsync_CallsTheInnerClientOnce_ForTheSameText()
    {
        var client = CreateClient("test-embedding");

        var first = await client.GenerateAsync("Reset links expire after one hour.");
        var second = await client.GenerateAsync("Reset links expire after one hour.");

        Assert.Equal(first.Vector, second.Vector);
        Assert.Equal(0, second.TokenCount);
        _inner.Verify(c => c.GenerateAsync("Reset links expire after one hour.", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_KeepsSeparateEntries_ForOtherTextsAndLengths()
    {
        var client = CreateClient("test-embedding");

        await client.GenerateAsync("Reset links expire after one hour.");
        await client.GenerateAsync("Accounts unlock after 30 minutes.");
        await client.GenerateAsync("Reset links expire after one hour.", new EmbeddingOptions { Dimensions = 128 });

        _inner.Verify(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<EmbeddingOptions?>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GenerateAsync_KeepsSeparateEntries_ForEachModel()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());

        await new CachingEmbeddingClient(_inner.Object, cache, "model-a").GenerateAsync("Reset links expire after one hour.");
        await new CachingEmbeddingClient(_inner.Object, cache, "model-b").GenerateAsync("Reset links expire after one hour.");

        _inner.Verify(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<EmbeddingOptions?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private CachingEmbeddingClient CreateClient(string modelId) =>
        new(_inner.Object, new MemoryCache(new MemoryCacheOptions()), modelId);
}
