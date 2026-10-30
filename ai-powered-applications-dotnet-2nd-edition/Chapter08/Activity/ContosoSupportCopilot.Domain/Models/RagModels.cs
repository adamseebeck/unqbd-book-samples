using ContosoSupportCopilot.Domain.Prompts;

namespace ContosoSupportCopilot.Domain.Models;

public record RagRequest
{
    public Guid TicketId { get; init; }
    public string Query { get; init; } = string.Empty;
    public string ProductArea { get; init; } = string.Empty;
    public string RequesterId { get; init; } = string.Empty;
    public int MaxResults { get; init; } = 5;

    // Null uses VectorStore:MinScore, the floor calibrated for the embedding model.
    public double? MinRelevanceScore { get; init; }

    // Null uses SupportPrompts.GroundedAnswer; Chapter 9 passes revised versions to compare them with a model.
    public PromptTemplate? Prompt { get; init; }
}

public record CitationReference
{
    public Guid ArticleId { get; init; }
    public string ArticleTitle { get; init; } = string.Empty;
    public int ChunkIndex { get; init; }

    // The masked, sanitized chunk text the model received. Store the excerpt, not this.
    public string Content { get; init; } = string.Empty;

    public string Excerpt { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
}

public record RagResult
{
    public string Response { get; init; } = string.Empty;

    // Every source the model received, with an excerpt for display.
    public IReadOnlyList<CitationReference> Citations { get; init; } = Array.Empty<CitationReference>();

    // The sources that the answer actually cites.
    public IReadOnlyList<Guid> CitedArticleIds { get; init; } = Array.Empty<Guid>();

    // The citation check: true when the answer ended normally, cites at least one source, and cites only sources
    // it received. It does not show that the cited text supports each sentence; Chapter 9 measures that.
    public bool IsGrounded { get; init; }

    // Why the answer ended, such as "stop", or "length" for an answer cut off at Rag:MaxOutputTokens.
    public string FinishReason { get; init; } = string.Empty;

    // True when the ticket itself contains text aimed at the model, such as "ignore previous instructions".
    // The answer is still produced from the escaped ticket, but a person reads it before anyone uses it.
    public bool RequiresReview { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int EmbeddingTokens { get; init; }
    public string ModelId { get; init; } = string.Empty;
}
