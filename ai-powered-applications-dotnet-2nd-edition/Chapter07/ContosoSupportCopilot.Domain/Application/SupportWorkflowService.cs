using System.Diagnostics;
using System.Security;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Application.Services;

public class SupportWorkflowService
{
    private const string PaymentsHandoff = "I understand you're asking about a payment. Let me connect you with a payments specialist.";
    private const string TechnicalFallback = "I'm experiencing technical difficulties. Please try again shortly.";

    // Another request for the same ticket can save first; the turn is then added again, up to this many times.
    private const int MaxSaveAttempts = 3;

    private readonly IIntentExtractor _intentExtractor;
    private readonly IDraftingService _draftingService;
    private readonly ISupportChatClient _chatClient;
    private readonly ISupportContext _supportContext;
    private readonly ICorrelationContext _correlationContext;
    private readonly IConversationRepository _conversationRepository;
    private readonly IAgentRunRepository _agentRunRepository;

    public SupportWorkflowService(
        IIntentExtractor intentExtractor,
        IDraftingService draftingService,
        ISupportChatClient chatClient,
        ISupportContext supportContext,
        ICorrelationContext correlationContext,
        IConversationRepository conversationRepository,
        IAgentRunRepository agentRunRepository)
    {
        _intentExtractor = intentExtractor;
        _draftingService = draftingService;
        _chatClient = chatClient;
        _supportContext = supportContext;
        _correlationContext = correlationContext;
        _conversationRepository = conversationRepository;
        _agentRunRepository = agentRunRepository;
    }

    public async Task<SupportWorkflowResult> ProcessTicketAsync(Guid ticketId, string userInput, CancellationToken cancellationToken = default)
    {
        // A caller's mistake, checked before the AI fallbacks below can mistake it for an outage.
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);
        var ticket = await _supportContext.GetTicketAsync(ticketId, cancellationToken)
            ?? throw new ArgumentException($"Ticket {ticketId} not found", nameof(ticketId));

        var (intent, errorMessage) = await ExtractIntentAsync(ticket, userInput, cancellationToken);
        var handOff = intent.Intent == "PaymentIssue" && intent.ConfidenceScore < 0.8;

        var stopwatch = Stopwatch.StartNew();
        DraftReply draft;

        try
        {
            draft = handOff
                ? new DraftReply { Text = PaymentsHandoff }
                : await _draftingService.DraftReplyAsync(ticket, new DraftingOptions(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // A second boundary for failures that get past FallbackDraftingService.
            draft = new DraftReply { Text = TechnicalFallback, IsFallback = true };
            errorMessage = errorMessage is null ? ex.Message : $"{errorMessage}; {ex.Message}";
        }

        stopwatch.Stop();

        var conversation = await SaveTurnAsync(ticketId, userInput, draft.Text, cancellationToken);

        // A handoff makes no model call, so its run is recorded under its own name.
        await _agentRunRepository.LogAsync(new AgentRun
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            TicketId = ticketId,
            CorrelationId = _correlationContext.CorrelationId,
            AgentName = handOff ? "PaymentsHandoff" : "DraftingAgent",
            InputTokens = draft.InputTokens,
            OutputTokens = draft.OutputTokens,
            LatencyMs = (int)stopwatch.ElapsedMilliseconds,
            ModelId = draft.ModelId,
            Timestamp = DateTime.UtcNow,
            IsFallback = draft.IsFallback,
            ErrorMessage = errorMessage
        }, cancellationToken);

        return new SupportWorkflowResult(draft.Text, intent, _correlationContext.CorrelationId, conversation.Id);
    }

    // An outage must not fail the request before the drafting fallback can answer. When extraction fails,
    // the ticket goes to manual review with its own priority, drafting still runs, and the run keeps the error.
    private async Task<(SupportIntent Intent, string? ErrorMessage)> ExtractIntentAsync(
        SupportTicket ticket,
        string userInput,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await _intentExtractor.ExtractAsync(userInput, cancellationToken), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            var manualReview = new SupportIntent { Intent = "ManualReview", Urgency = ticket.Priority, ProductArea = ticket.ProductArea };
            return (manualReview, $"Intent extraction failed: {ex.Message}");
        }
    }

    // Adds the turn to the ticket's active conversation, or starts one. When another request for the same
    // ticket saved first, the repository reports a conflict, and the turn is added again to the conversation
    // as it is now. The draft is not generated twice.
    private async Task<CopilotConversation> SaveTurnAsync(
        Guid ticketId,
        string userInput,
        string response,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var conversation = await _conversationRepository.GetByTicketAsync(ticketId, cancellationToken)
                ?? new CopilotConversation
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticketId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

            var turn = new ConversationTurn
            {
                TurnNumber = conversation.CurrentTurn + 1,
                UserInput = userInput,
                CopilotResponse = response,
                Timestamp = DateTime.UtcNow
            };

            // Records are immutable, but their lists are not: build a new list instead of changing the old one.
            var updated = conversation with
            {
                CurrentTurn = turn.TurnNumber,
                UpdatedAt = DateTime.UtcNow,
                Turns = [.. conversation.Turns, turn]
            };

            try
            {
                await _conversationRepository.SaveAsync(updated, cancellationToken);
                return updated;
            }
            catch (ConversationConflictException) when (attempt < MaxSaveAttempts)
            {
                // The next pass loads the other request's turn and adds this one after it.
            }
        }
    }

    public async Task<SupportCaseSummary> SummarizeTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _supportContext.GetTicketAsync(ticketId, cancellationToken)
            ?? throw new ArgumentException($"Ticket {ticketId} not found", nameof(ticketId));

        var messages = await _supportContext.GetTicketMessagesAsync(ticketId, cancellationToken);

        // Ticket text is escaped, as in DraftingService, so no value can close the tags around it.
        var thread = string.Join("\n", messages.Select(m =>
            $"{(m.IsFromCustomer ? "Customer" : "Support")}: {SecurityElement.Escape(m.Content)}"));
        var prompt = $"""
            <subject>{SecurityElement.Escape(ticket.Subject)}</subject>
            <status>{SecurityElement.Escape(ticket.Status)}</status>
            <description>{SecurityElement.Escape(ticket.Description)}</description>
            <ticket_thread>
            {thread}
            </ticket_thread>
            """;
        var options = new SupportChatOptions
        {
            SystemPrompt = "Summarize the support ticket thread for an engineer: the issue, its status, and the next step.",
            MaxOutputTokens = 2000
        };

        var stopwatch = Stopwatch.StartNew();
        var response = await _chatClient.CompleteAsync(prompt, options, cancellationToken);
        stopwatch.Stop();

        await _agentRunRepository.LogAsync(new AgentRun
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.Empty,
            TicketId = ticketId,
            CorrelationId = _correlationContext.CorrelationId,
            AgentName = "SummarizationAgent",
            InputTokens = response.InputTokens,
            OutputTokens = response.OutputTokens,
            LatencyMs = (int)stopwatch.ElapsedMilliseconds,
            ModelId = response.ModelId,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return new SupportCaseSummary
        {
            TicketId = ticketId,
            Summary = response.Content,
            GeneratedAt = DateTime.UtcNow,
            InputTokens = response.InputTokens,
            OutputTokens = response.OutputTokens
        };
    }
}
