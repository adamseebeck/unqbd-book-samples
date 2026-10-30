using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Workflows;

namespace ContosoSupportCopilot.Application.Workflows;

// A state machine over the Chapter 10 agents. Every transition is saved before the next agent runs,
// so a stopped run continues from its last step instead of starting over.
public class SupportTicketWorkflow : ISupportWorkflow
{
    private readonly IAgentCoordinator _coordinator;
    private readonly IConversationRepository _conversationRepository;
    private readonly IWorkflowStateRepository _stateRepository;
    private readonly ICorrelationContext _correlationContext;
    private readonly ISupportContext _supportContext;
    private readonly WorkflowRetryPolicy _retryPolicy;

    public SupportTicketWorkflow(
        IAgentCoordinator coordinator,
        IConversationRepository conversationRepository,
        IWorkflowStateRepository stateRepository,
        ICorrelationContext correlationContext,
        ISupportContext supportContext,
        WorkflowRetryPolicy retryPolicy)
    {
        _coordinator = coordinator;
        _conversationRepository = conversationRepository;
        _stateRepository = stateRepository;
        _correlationContext = correlationContext;
        _supportContext = supportContext;
        _retryPolicy = retryPolicy;
    }

    public async Task<WorkflowResult> ExecuteAsync(Guid ticketId, string engineerId, string initialInput,
        string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineerId);

        // A retried request returns the first run: finished, it reads the stored result; stopped, it continues.
        if (!string.IsNullOrWhiteSpace(idempotencyKey)
            && await _stateRepository.FindByIdempotencyKeyAsync(engineerId, idempotencyKey, cancellationToken) is { } existing)
        {
            // A key names one request. Sent again with another ticket or input, it would return the wrong run.
            if (await _stateRepository.GetAsync(existing, cancellationToken) is { } first
                && (first.TicketId != ticketId || !string.Equals(first.InitialInput, initialInput, StringComparison.Ordinal)))
            {
                throw new IdempotencyKeyReusedException(idempotencyKey);
            }

            return await ResumeAsync(existing, engineerId, cancellationToken);
        }
        var ticket = await GetTicketAsync(ticketId, cancellationToken);
        var conversation = await EnsureConversationAsync(ticketId, cancellationToken);

        var state = new WorkflowState
        {
            TicketId = ticketId,
            ConversationId = conversation.Id,
            CorrelationId = _correlationContext.CorrelationId,
            RequesterId = engineerId,
            InitialInput = initialInput
        };
        state = await _stateRepository.SaveAsync(state, cancellationToken);

        // Two requests with the same key at the same moment: the database keeps one, the other stops here.
        if (!string.IsNullOrWhiteSpace(idempotencyKey)
            && !await _stateRepository.TryRecordIdempotencyKeyAsync(engineerId, idempotencyKey, state.WorkflowId, cancellationToken))
        {
            throw new WorkflowConflictException($"A request with the idempotency key '{idempotencyKey}' is already being processed.");
        }

        return await RunAsync(state, ticket, cancellationToken);
    }

    public async Task<WorkflowResult> ResumeAsync(Guid workflowId, string engineerId, CancellationToken cancellationToken = default)
    {
        // A workflow that another engineer started is reported as missing, so its ID reveals nothing.
        var state = await _stateRepository.GetAsync(workflowId, cancellationToken);
        if (state is null || !string.Equals(state.RequesterId, engineerId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Workflow {workflowId} not found", nameof(workflowId));
        }

        if (state.CurrentStep is WorkflowStep.Completed or WorkflowStep.Failed)
        {
            return ToResult(state);
        }

        // A run that a transient failure stopped continues with the step that failed: checkpointed retry.
        state = state.WithoutArtifact(WorkflowArtifactKeys.RetryAfter);

        // The agents and run records of the resumed run use the correlation ID of the run it continues.
        // Set inside this async method, the ID does not flow back to the caller (Chapter 6): the resume
        // request's own log scope and X-Correlation-ID header keep the ID of that request.
        _correlationContext.SetCorrelationId(state.CorrelationId);
        return await RunAsync(state, await GetTicketAsync(state.TicketId, cancellationToken), cancellationToken);
    }

    private async Task<WorkflowResult> RunAsync(WorkflowState state, SupportTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            // A transient failure ends the loop too, with the run stopped at its step until the caller resumes it.
            while (state.CurrentStep is not (WorkflowStep.Completed or WorkflowStep.Failed) && !state.HasArtifact(WorkflowArtifactKeys.RetryAfter))
            {
                state = state.CurrentStep switch
                {
                    WorkflowStep.Triage => await TriageAsync(state, ticket, cancellationToken),
                    WorkflowStep.KnowledgeRetrieval => await RetrieveKnowledgeAsync(state, ticket, cancellationToken),
                    WorkflowStep.Drafting => await DraftAsync(state, ticket, cancellationToken),
                    _ => await EscalateAsync(state, ticket, cancellationToken)
                };

                // Durable before the next agent runs: a restart continues from here.
                state = await _stateRepository.SaveAsync(state, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or WorkflowConflictException))
        {
            // A conflict is not stored as a failure: the other request owns the workflow now. A failed run
            // never resumes, so it drops a time to resume that a stop just before the failure added.
            state = state.WithoutArtifact(WorkflowArtifactKeys.RetryAfter)
                .WithArtifact(WorkflowArtifactKeys.Error, ex.Message).WithStep(WorkflowStep.Failed);
            state = await _stateRepository.SaveAsync(state, CancellationToken.None);
        }

        return ToResult(state);
    }

    private async Task<WorkflowState> TriageAsync(WorkflowState state, SupportTicket ticket, CancellationToken cancellationToken)
    {
        var result = await RunAgentAsync(AgentNames.Triage, state, ticket, cancellationToken);
        if (HandleFailure(AgentNames.Triage, result, state) is { } next)
        {
            return next;
        }

        state = state.WithArtifact(WorkflowArtifactKeys.Triage, new TriageOutcome(
            Text(result, AgentArtifactKeys.Intent), Text(result, AgentArtifactKeys.Urgency), Text(result, AgentArtifactKeys.ProductArea),
            Number(result, AgentArtifactKeys.Confidence) ?? 0, Number(result, AgentArtifactKeys.PriorityScore)));

        // An uncertain classification goes to engineering before anyone writes to the customer.
        return result.RequiresHandoff ? HandOff(state, AgentNames.Triage) : state.WithStep(WorkflowStep.KnowledgeRetrieval);
    }

    private async Task<WorkflowState> RetrieveKnowledgeAsync(WorkflowState state, SupportTicket ticket, CancellationToken cancellationToken)
    {
        var result = await RunAgentAsync(AgentNames.Knowledge, state, ticket, cancellationToken);
        if (HandleFailure(AgentNames.Knowledge, result, state) is { } next)
        {
            return next;
        }

        // Kept even when ungrounded, so the engineer who takes over sees what retrieval found.
        var citations = result.Artifacts.GetValueOrDefault(AgentArtifactKeys.Citations) as IReadOnlyList<CitationReference> ?? [];
        state = state.WithArtifact(WorkflowArtifactKeys.Knowledge, new KnowledgeOutcome(result.Response,
            result.Artifacts.GetValueOrDefault(AgentArtifactKeys.IsGrounded) is true, citations, Text(result, AgentArtifactKeys.SearchQuery)));

        return result.RequiresHandoff ? HandOff(state, AgentNames.Knowledge) : state.WithStep(WorkflowStep.Drafting);
    }

    private async Task<WorkflowState> DraftAsync(WorkflowState state, SupportTicket ticket, CancellationToken cancellationToken)
    {
        // The knowledge step's cited answer becomes the draft's facts, as in Chapter 10's handoff.
        var knowledge = state.GetArtifact<KnowledgeOutcome>(WorkflowArtifactKeys.Knowledge)
            ?? throw new InvalidOperationException("The drafting step needs the knowledge outcome.");
        var result = await RunAgentAsync(AgentNames.Drafting, state, ticket, cancellationToken,
            new DraftingKnowledge(knowledge.Answer, knowledge.Citations));
        if (HandleFailure(AgentNames.Drafting, result, state) is { } next)
        {
            return next;
        }

        // A fallback reply is a placeholder, so a person takes the ticket.
        state = state.WithArtifact(WorkflowArtifactKeys.Draft,
            new DraftOutcome(result.Response, result.RequiresHandoff, Text(result, AgentArtifactKeys.ModelId)));
        return result.RequiresHandoff ? HandOff(state, AgentNames.Drafting) : state.WithStep(WorkflowStep.Completed);
    }

    private async Task<WorkflowState> EscalateAsync(WorkflowState state, SupportTicket ticket, CancellationToken cancellationToken)
    {
        var result = await RunAgentAsync(AgentNames.Escalation, state, ticket, cancellationToken);
        if (result.Artifacts.GetValueOrDefault(AgentArtifactKeys.FailureMode) is AgentFailureMode mode)
        {
            // Escalation is the last resort: stop for a later retry on a transient failure, end the run on any other.
            return mode == AgentFailureMode.Transient && state.RetryCount < _retryPolicy.MaxRetries
                ? StopForRetry(state)
                : state.WithArtifact(WorkflowArtifactKeys.Error, $"{AgentNames.Escalation}: {result.Response}").WithStep(WorkflowStep.Failed);
        }

        var draft = result.Artifacts.GetValueOrDefault(AgentArtifactKeys.EscalationDraft) as EscalationDraft
            ?? throw new InvalidOperationException("EscalationAgent returned no escalation draft.");
        return state.WithArtifact(WorkflowArtifactKeys.Escalation, new EscalationOutcome(draft, Number(result, AgentArtifactKeys.PriorityScore)))
            .WithStep(WorkflowStep.Completed);
    }

    // Transient: stop at this step, keep what earlier steps produced, and let a later resume run the step again.
    // Unrecoverable: stop. Authorization, content safety and exhausted retries: a person decides, so the ticket
    // goes to escalation.
    private WorkflowState? HandleFailure(string agentName, AgentResult result, WorkflowState state)
    {
        if (result.Artifacts.GetValueOrDefault(AgentArtifactKeys.FailureMode) is not AgentFailureMode mode)
        {
            return null;
        }

        return mode switch
        {
            AgentFailureMode.Transient when state.RetryCount < _retryPolicy.MaxRetries => StopForRetry(state),
            AgentFailureMode.Unrecoverable => state.WithArtifact(WorkflowArtifactKeys.Error, $"{agentName}: {result.Response}").WithStep(WorkflowStep.Failed),
            _ => HandOff(state, agentName)
        };
    }

    // No retry inside this request: the agent's model calls already passed the Chapter 4 pipeline, and running
    // the step again at once would repeat every call of the agent while the provider is still failing.
    private WorkflowState StopForRetry(WorkflowState state) =>
        state.IncrementRetry().WithArtifact(WorkflowArtifactKeys.RetryAfter, DateTime.UtcNow + _retryPolicy.RetryAfter);

    private static WorkflowState HandOff(WorkflowState state, string fromAgent) =>
        state.WithArtifact(WorkflowArtifactKeys.HandoffFrom, fromAgent).WithStep(WorkflowStep.Escalation);

    private Task<AgentResult> RunAgentAsync(string agentName, WorkflowState state, SupportTicket ticket,
        CancellationToken cancellationToken, DraftingKnowledge? knowledge = null)
    {
        var context = new AgentContext
        {
            ConversationId = state.ConversationId,
            CurrentTicket = ticket,
            Intent = state.CurrentStep.ToString(),
            RequesterId = state.RequesterId,
            Knowledge = knowledge,
            Metadata = { ["input"] = state.InitialInput, ["workflow_id"] = state.WorkflowId }
        };
        return _coordinator.ExecuteAgentAsync(agentName, context, cancellationToken);
    }

    private static WorkflowResult ToResult(WorkflowState state)
    {
        var escalated = state.HasArtifact(WorkflowArtifactKeys.HandoffFrom);

        // Only a stopped run can be resumed, so a finished one never tells the caller to come back.
        var finished = state.CurrentStep is WorkflowStep.Completed or WorkflowStep.Failed;
        DateTime? retryAfter = !finished && state.HasArtifact(WorkflowArtifactKeys.RetryAfter)
            ? state.GetArtifact<DateTime>(WorkflowArtifactKeys.RetryAfter)
            : null;
        var finalResponse = escalated
            ? state.GetArtifact<EscalationOutcome>(WorkflowArtifactKeys.Escalation)?.Draft.Summary
            : state.GetArtifact<DraftOutcome>(WorkflowArtifactKeys.Draft)?.Text;

        return new WorkflowResult
        {
            IsSuccess = state.CurrentStep == WorkflowStep.Completed,
            FinalResponse = finalResponse ?? string.Empty,
            Citations = state.GetArtifact<KnowledgeOutcome>(WorkflowArtifactKeys.Knowledge)?.Citations ?? [],
            EscalationRequired = escalated,
            ErrorMessage = state.GetArtifact<string>(WorkflowArtifactKeys.Error),
            FinalStep = state.CurrentStep,
            RetryAfter = retryAfter,
            WorkflowId = state.WorkflowId,
            CorrelationId = state.CorrelationId
        };
    }

    private async Task<SupportTicket> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken) =>
        await _supportContext.GetTicketAsync(ticketId, cancellationToken)
            ?? throw new ArgumentException($"Ticket {ticketId} not found", nameof(ticketId));

    private async Task<CopilotConversation> EnsureConversationAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByTicketAsync(ticketId, cancellationToken);
        if (conversation is not null)
        {
            return conversation;
        }

        // Saved right away: an unsaved conversation would leave every agent run without a parent.
        conversation = new CopilotConversation { Id = Guid.NewGuid(), TicketId = ticketId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        try
        {
            await _conversationRepository.SaveAsync(conversation, cancellationToken);
            return conversation;
        }
        catch (ConversationConflictException)
        {
            // Another request created the ticket's conversation first (Chapter 6's unique index): use that one.
            return await _conversationRepository.GetByTicketAsync(ticketId, cancellationToken)
                ?? throw new InvalidOperationException($"Ticket {ticketId} has no active conversation.");
        }
    }

    private static string Text(AgentResult result, string key) => result.Artifacts.GetValueOrDefault(key)?.ToString() ?? string.Empty;

    private static double? Number(AgentResult result, string key) => result.Artifacts.GetValueOrDefault(key) as double?;
}
