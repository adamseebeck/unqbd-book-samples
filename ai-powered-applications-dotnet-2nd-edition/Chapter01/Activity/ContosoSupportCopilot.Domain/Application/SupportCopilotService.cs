using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Application.Services;

public class SupportCopilotService
{
    private readonly ISupportContext _supportContext;
    private readonly ISupportChatClient _chatClient;
    private readonly IKernelOrchestrator _kernelOrchestrator;
    private readonly IAgentCoordinator _agentCoordinator;

    public SupportCopilotService(
        ISupportContext supportContext,
        ISupportChatClient chatClient,
        IKernelOrchestrator kernelOrchestrator,
        IAgentCoordinator agentCoordinator)
    {
        _supportContext = supportContext;
        _chatClient = chatClient;
        _kernelOrchestrator = kernelOrchestrator;
        _agentCoordinator = agentCoordinator;
    }

    public async Task<string> DraftReplyAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _supportContext.GetTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            throw new ArgumentException($"Ticket {ticketId} not found", nameof(ticketId));
        }

        var articles = await _supportContext.SearchKnowledgeBaseAsync(ticket.Subject, ticket.ProductArea, cancellationToken);
        var articlesText = string.Join("\n", articles.Select(a => $"{a.Title}: {a.Content[..Math.Min(200, a.Content.Length)]}"));

        var context = new Dictionary<string, object>
        {
            ["ticketSubject"] = ticket.Subject,
            ["ticketDescription"] = ticket.Description,
            ["productArea"] = ticket.ProductArea,
            ["relevantArticles"] = articlesText
        };

        return await _kernelOrchestrator.ExecutePromptAsync(
            "Draft a professional support response for the following ticket about {{$productArea}}.\n\n" +
            "Subject: {{$ticketSubject}}\nDescription: {{$ticketDescription}}\n\n" +
            "Consider these knowledge articles: {{$relevantArticles}}",
            context,
            cancellationToken);
    }

    public async Task<SupportCaseSummary> SummarizeTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _supportContext.GetTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            throw new ArgumentException($"Ticket {ticketId} not found", nameof(ticketId));
        }

        var messages = await _supportContext.GetTicketMessagesAsync(ticketId, cancellationToken);
        var messageHistory = string.Join("\n", messages.Select(m => $"{(m.IsFromCustomer ? "Customer" : "Agent")}: {m.Content}"));

        var prompt = $"Summarize the following support ticket concisely. Identify the key issue, current status, and recommended next steps.\n\nSubject: {ticket.Subject}\nDescription: {ticket.Description}\nStatus: {ticket.Status}\n\nMessage History:\n{messageHistory}";

        var response = await _chatClient.CompleteAsync(prompt, new SupportChatOptions { MaxOutputTokens = 2000 }, cancellationToken);

        return new SupportCaseSummary
        {
            TicketId = ticketId,
            Summary = response.Content,
            GeneratedAt = DateTime.UtcNow,
            InputTokens = response.InputTokens,
            OutputTokens = response.OutputTokens
        };
    }

    public async Task<AgentResult> TriageTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _supportContext.GetTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            throw new ArgumentException($"Ticket {ticketId} not found", nameof(ticketId));
        }

        var context = new AgentContext
        {
            ConversationId = Guid.NewGuid(),
            CurrentTicket = ticket,
            Intent = "triage",
            Metadata = { ["priority"] = ticket.Priority }
        };

        return await _agentCoordinator.ExecuteAgentAsync("TriageAgent", context, cancellationToken);
    }

    public async Task<EscalationDraft> DraftEscalationAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _supportContext.GetTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            throw new ArgumentException($"Ticket {ticketId} not found", nameof(ticketId));
        }

        var variables = new Dictionary<string, object>
        {
            ["subject"] = ticket.Subject,
            ["description"] = ticket.Description,
            ["priority"] = ticket.Priority,
            ["productArea"] = ticket.ProductArea
        };

        var analysis = await _kernelOrchestrator.ExecutePromptAsync(
            "Prepare an engineering escalation for this {{$productArea}} ticket.\n" +
            "Priority: {{$priority}}\nSubject: {{$subject}}\nDescription: {{$description}}\n\n" +
            "Summarize the technical problem, list the reproduction steps you can infer, and state the business impact.",
            variables,
            cancellationToken);

        var draft = new EscalationDraft
        {
            TicketId = ticket.Id,
            EngineeringSummary = analysis,
            ReproductionSteps = ticket.Description,
            BusinessImpact = $"{ticket.Priority} priority ticket in {ticket.ProductArea}"
        };

        await _supportContext.SaveEscalationDraftAsync(draft, cancellationToken);
        return draft;
    }
}
