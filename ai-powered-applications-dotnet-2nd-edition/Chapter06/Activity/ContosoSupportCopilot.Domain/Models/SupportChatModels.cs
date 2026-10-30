namespace ContosoSupportCopilot.Domain.Models;

public record SupportChatResponse
{
    public string Content { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public string ModelId { get; init; } = string.Empty;
    public string FinishReason { get; init; } = string.Empty;
}

public record SupportChatChunk
{
    public string Content { get; init; } = string.Empty;
    public bool IsComplete { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
}

public record SupportChatOptions
{
    public string? SystemPrompt { get; init; }
    public int? MaxOutputTokens { get; init; }
    public float? Temperature { get; init; }
}
