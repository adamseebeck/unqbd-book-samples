namespace ContosoSupportCopilot.Domain.Models;

public record CopilotConversation
{
    public Guid Id { get; init; }
    public Guid TicketId { get; init; }
    public int CurrentTurn { get; init; }
    public string ConversationState { get; init; } = "Active";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<ConversationTurn> Turns { get; init; } = new();
}

public record ConversationTurn
{
    public int TurnNumber { get; init; }
    public string UserInput { get; init; } = string.Empty;
    public string? CopilotResponse { get; init; }
    public DateTime Timestamp { get; init; }
}
