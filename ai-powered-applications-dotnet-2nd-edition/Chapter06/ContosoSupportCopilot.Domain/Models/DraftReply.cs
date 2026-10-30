namespace ContosoSupportCopilot.Domain.Models;

public record DraftReply
{
    public string Text { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public bool IsFallback { get; init; }
}
