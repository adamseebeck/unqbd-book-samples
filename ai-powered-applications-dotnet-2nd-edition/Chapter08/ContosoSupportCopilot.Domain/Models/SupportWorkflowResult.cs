namespace ContosoSupportCopilot.Domain.Models;

public record SupportWorkflowResult(
    string Response,
    SupportIntent Intent,
    string CorrelationId,
    Guid ConversationId);
