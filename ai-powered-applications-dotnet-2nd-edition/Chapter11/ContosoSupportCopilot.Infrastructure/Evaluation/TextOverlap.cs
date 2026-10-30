using System.Text.RegularExpressions;

namespace ContosoSupportCopilot.Infrastructure.Evaluation;

// Word overlap between an answer and its sources: a fast, deterministic signal that the answer reuses
// the sources' words. It cannot tell a paraphrase from a new claim, and it misses contradictions: words
// under four letters, such as "not" and numbers below 1,000, are ignored, and word order never counts.
// The groundedness judge checks meaning.
public static partial class TextOverlap
{
    private static readonly HashSet<string> StopWords =
    [
        "about", "after", "also", "been", "before", "both", "from", "have", "into", "more", "most",
        "only", "should", "than", "that", "their", "them", "then", "there", "they", "this", "when",
        "where", "which", "will", "with", "would", "your"
    ];

    // Sentences of the answer, without bracketed labels such as [Article: Title (ID)].
    public static IReadOnlyList<string> Sentences(string text) =>
        SentenceBoundary().Split(Bracketed().Replace(text, " "))
            .Select(sentence => sentence.Trim())
            .Where(sentence => ContentWords(sentence).Count >= 2)
            .ToList();

    public static HashSet<string> ContentWords(string text) =>
        Word().Matches(text.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(word => word.Length >= 4 && !StopWords.Contains(word))
            .ToHashSet();

    // The share of the sentence's content words that also appear in the source words.
    public static double Coverage(string sentence, HashSet<string> sourceWords)
    {
        var words = ContentWords(sentence);
        return words.Count == 0 ? 1.0 : (double)words.Count(sourceWords.Contains) / words.Count;
    }

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBoundary();

    [GeneratedRegex(@"[[][^]]*[]]")]
    private static partial Regex Bracketed();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Word();
}
