namespace ContosoSupportCopilot.Domain.Repositories;

// Another request saved the ticket's conversation first. Load it again and repeat the change.
public class ConversationConflictException : Exception
{
    public ConversationConflictException(Guid ticketId, Exception innerException)
        : base($"Another request changed the conversation for ticket {ticketId} first.", innerException)
    {
        TicketId = ticketId;
    }

    public Guid TicketId { get; }
}
