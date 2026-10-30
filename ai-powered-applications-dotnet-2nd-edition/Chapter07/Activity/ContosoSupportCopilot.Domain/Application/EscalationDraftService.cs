using System.Security;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Validation;

namespace ContosoSupportCopilot.Application.Services;

public class EscalationDraftService
{
    private readonly ISupportContext _supportContext;
    private readonly IKernelOrchestrator _orchestrator;
    private readonly IValidatedPromptExecutor _executor;
    private readonly IOutputValidator<EscalationDraft> _validator;

    public EscalationDraftService(
        ISupportContext supportContext,
        IKernelOrchestrator orchestrator,
        IValidatedPromptExecutor executor,
        IOutputValidator<EscalationDraft> validator)
    {
        _supportContext = supportContext;
        _orchestrator = orchestrator;
        _executor = executor;
        _validator = validator;
    }

    public async Task<EscalationDraft> GenerateDraftAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _supportContext.GetTicketAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new ArgumentException($"Ticket {ticketId} not found", nameof(ticketId));
        }

        // The Chapter 4 plugin still calculates the score through Semantic Kernel.
        var priorityScore = await _orchestrator.ExecutePluginFunctionAsync(
            "SupportTicketPlugin",
            "calculate_priority_score",
            new Dictionary<string, object> { ["ticketId"] = ticketId },
            cancellationToken);

        // The template puts these values between XML tags, so text from the ticket is escaped.
        var variables = new Dictionary<string, object>
        {
            ["subject"] = SecurityElement.Escape(ticket.Subject),
            ["description"] = SecurityElement.Escape(ticket.Description),
            ["productArea"] = SecurityElement.Escape(ticket.ProductArea),
            ["customerTier"] = SecurityElement.Escape(ticket.Customer?.Tier ?? "Unknown"),
            ["priorityScore"] = priorityScore
        };

        return await _executor.ExecuteAsync(SupportPrompts.Escalation, variables, _validator, cancellationToken);
    }
}
