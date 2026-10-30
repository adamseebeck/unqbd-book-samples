namespace ContosoSupportCopilot.Domain.Models;

public enum AgentFailureMode
{
    Transient,
    Authorization,
    ContentSafety,
    Unrecoverable
}

public record AgentToolResult
{
    public bool IsSuccess { get; init; }

    // The typed result for the agent that called the tool.
    public object? Value { get; init; }

    // What the model reads; null sends Value. Keep it small: every token is paid for again.
    public object? ModelOutput { get; init; }

    public string? ErrorMessage { get; init; }
    public AgentFailureMode FailureMode { get; init; }

    public static AgentToolResult Success(object? value, object? modelOutput = null) =>
        new() { IsSuccess = true, Value = value, ModelOutput = modelOutput };

    public static AgentToolResult Failure(AgentFailureMode mode, string message) =>
        new() { IsSuccess = false, FailureMode = mode, ErrorMessage = message };
}

// What the application, not the model, decides: which ticket, which conversation, and who is asking.
public class AgentToolContext
{
    public string CorrelationId { get; }
    public Guid ConversationId { get; }
    public CopilotConversation? Conversation { get; }
    public SupportTicket? CurrentTicket { get; }
    public string RequesterId { get; }

    // What the knowledge agent found for this ticket, when the run drafts a reply from it.
    public DraftingKnowledge? Knowledge { get; }

    public AgentToolContext(string correlationId, Guid conversationId,
        CopilotConversation? conversation, SupportTicket? currentTicket, string requesterId,
        DraftingKnowledge? knowledge = null)
    {
        CorrelationId = correlationId;
        ConversationId = conversationId;
        Conversation = conversation;
        CurrentTicket = currentTicket;
        RequesterId = requesterId;
        Knowledge = knowledge;
    }
}

// The input of a tool that needs nothing from the model.
public sealed record NoToolInput;

// The arguments the model supplies when it searches the knowledge base.
public sealed record KnowledgeSearchInput(string Query);
