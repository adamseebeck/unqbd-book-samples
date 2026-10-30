namespace ContosoSupportCopilot.Domain.Models;

public record SupportTicket
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = "Open";
    public string Priority { get; init; } = "Normal";
    public string ProductArea { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public List<TicketMessage> Messages { get; init; } = new();
    public CustomerAccount? Customer { get; init; }
}

public record TicketMessage
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsFromCustomer { get; init; }
    public DateTime SentAt { get; init; }
}

public record CustomerAccount
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
}

public record SupportCaseSummary
{
    public Guid TicketId { get; init; }
    public string Summary { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
}
