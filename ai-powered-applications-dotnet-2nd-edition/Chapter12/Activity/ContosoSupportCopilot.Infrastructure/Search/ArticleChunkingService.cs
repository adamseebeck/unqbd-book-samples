using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Search;

public sealed partial class ArticleChunkingService : IArticleChunkingService
{
    public Task<IReadOnlyList<ArticleChunk>> ChunkAsync(
        KnowledgeArticle article,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ChunkingOptions();
        if (options.MaxChunkSize < 100)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxChunkSize must be at least 100 characters.");
        }

        if (options.OverlapSize < 0 || options.OverlapSize > options.MaxChunkSize / 2)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "OverlapSize must be between 0 and half of MaxChunkSize.");
        }

        // Pieces leave room for the overlap and a separator, so every chunk stays within MaxChunkSize.
        var pieceLimit = options.MaxChunkSize - options.OverlapSize - 1;
        var chunks = new List<ArticleChunk>();
        var current = new StringBuilder();

        foreach (var paragraph in Paragraphs(article.Content))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // A paragraph that fits stays whole; a longer one is split at sentence boundaries.
            IEnumerable<string> pieces = options.PreserveParagraphBoundaries && paragraph.Length <= options.MaxChunkSize
                ? [paragraph]
                : SplitIntoPieces(paragraph, pieceLimit);

            var separator = "\n";
            foreach (var piece in pieces)
            {
                if (current.Length > 0 && current.Length + separator.Length + piece.Length > options.MaxChunkSize)
                {
                    var previous = current.ToString();
                    chunks.Add(CreateChunk(article.Id, chunks.Count, previous));
                    current.Clear();

                    // Start the next chunk with the end of the previous one, so a fact that spans
                    // the boundary is still found by a query that matches either side.
                    var overlap = Tail(previous, options.OverlapSize);
                    if (overlap.Length > 0 && overlap.Length + separator.Length + piece.Length <= options.MaxChunkSize)
                    {
                        current.Append(overlap);
                    }
                }

                if (current.Length > 0)
                {
                    current.Append(separator);
                }

                current.Append(piece);
                separator = " ";
            }
        }

        if (current.Length > 0)
        {
            chunks.Add(CreateChunk(article.Id, chunks.Count, current.ToString()));
        }

        return Task.FromResult<IReadOnlyList<ArticleChunk>>(chunks);
    }

    private static IEnumerable<string> Paragraphs(string content) =>
        BlankLine().Split(content.ReplaceLineEndings("\n"))
            .Select(paragraph => Whitespace().Replace(paragraph, " ").Trim())
            .Where(paragraph => paragraph.Length > 0);

    private static IEnumerable<string> SplitIntoPieces(string paragraph, int limit)
    {
        foreach (var sentence in SentenceBoundary().Split(paragraph))
        {
            var rest = sentence;

            // A sentence longer than the limit is cut at the last space that fits; only a single
            // word longer than the limit has to be cut inside.
            while (rest.Length > limit)
            {
                var cut = rest.LastIndexOf(' ', limit);
                if (cut <= 0)
                {
                    cut = limit;
                }

                yield return rest[..cut];
                rest = rest[cut..].TrimStart();
            }

            if (rest.Length > 0)
            {
                yield return rest;
            }
        }
    }

    // The last characters of a chunk, starting at a word boundary.
    private static string Tail(string text, int size)
    {
        if (size == 0 || text.Length <= size)
        {
            return size == 0 ? string.Empty : text;
        }

        var start = text.IndexOfAny([' ', '\n'], text.Length - size);
        return start < 0 ? string.Empty : text[(start + 1)..];
    }

    private static ArticleChunk CreateChunk(Guid articleId, int index, string content) => new()
    {
        // The same article and position always get the same ID, so indexing an article again
        // replaces its chunks instead of adding copies.
        Id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{articleId:N}:{index}")).AsSpan(0, 16)),
        Content = content.Trim(),
        ChunkIndex = index
    };

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLine();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBoundary();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
