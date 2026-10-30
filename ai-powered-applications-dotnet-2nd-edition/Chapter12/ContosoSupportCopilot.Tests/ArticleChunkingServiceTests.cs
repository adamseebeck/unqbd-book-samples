using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Search;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class ArticleChunkingServiceTests
{
    private readonly ArticleChunkingService _service = new();
    private readonly KnowledgeArticle _signInArticle = SampleData.CreateArticles().Single(a => a.Id == SampleData.SignInErrorsArticleId);

    [Fact]
    public async Task ChunkAsync_KeepsChunksWithinTheLimit_AndEndsThemAtSentences()
    {
        var chunks = await _service.ChunkAsync(_signInArticle);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Content.Length <= 512, $"chunk {chunk.ChunkIndex} has {chunk.Content.Length} characters"));
        Assert.All(chunks, chunk => Assert.EndsWith(".", chunk.Content));
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(c => c.ChunkIndex));
    }

    [Fact]
    public async Task ChunkAsync_StartsEachChunk_WithTheEndOfThePreviousOne()
    {
        var chunks = await _service.ChunkAsync(_signInArticle, new ChunkingOptions { OverlapSize = 50 });

        foreach (var (previous, next) in chunks.Zip(chunks.Skip(1)))
        {
            var firstWords = string.Join(' ', next.Content.Split(' ').Take(3));
            Assert.Contains(firstWords, previous.Content[^50..]);
        }
    }

    [Fact]
    public async Task ChunkAsync_CutsALongSentence_AtAWordBoundary()
    {
        var words = Enumerable.Range(1, 150).Select(i => $"word{i}");
        var article = new KnowledgeArticle { Id = Guid.NewGuid(), Title = "Long", Content = string.Join(' ', words) };

        var chunks = await _service.ChunkAsync(article, new ChunkingOptions { MaxChunkSize = 200, OverlapSize = 20 });

        var allWords = words.ToHashSet();
        Assert.All(chunks, chunk => Assert.All(chunk.Content.Split(' '), word => Assert.Contains(word, allWords)));
        Assert.All(chunks, chunk => Assert.True(chunk.Content.Length <= 200));
    }

    [Fact]
    public async Task ChunkAsync_KeepsAShortParagraphInOneChunk()
    {
        var first = new string('a', 300) + ".";
        var second = "The second paragraph must stay together, even though it does not fit after the first one.";
        var article = new KnowledgeArticle { Id = Guid.NewGuid(), Title = "Paragraphs", Content = $"{first}\n\n{second}" };

        var chunks = await _service.ChunkAsync(article, new ChunkingOptions { MaxChunkSize = 350, OverlapSize = 0 });

        Assert.Equal([first, second], chunks.Select(c => c.Content));
    }

    [Fact]
    public async Task ChunkAsync_GivesTheSameIds_WhenAnArticleIsChunkedAgain()
    {
        var first = await _service.ChunkAsync(_signInArticle);
        var second = await _service.ChunkAsync(_signInArticle);
        var other = await _service.ChunkAsync(_signInArticle with { Id = Guid.NewGuid() });

        Assert.Equal(first.Select(c => c.Id), second.Select(c => c.Id));
        Assert.Empty(first.Select(c => c.Id).Intersect(other.Select(c => c.Id)));
    }

    [Fact]
    public async Task ChunkAsync_RejectsAnOverlapOfMoreThanHalfTheChunkSize()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.ChunkAsync(_signInArticle, new ChunkingOptions { MaxChunkSize = 200, OverlapSize = 150 }));
    }
}
