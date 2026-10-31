using System.Diagnostics;
using System.Text.Json;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ContosoSupportCopilot.Infrastructure.Agents;

public sealed record AgentRunOutcome
{
    public AgentResponse? Response { get; init; }
    public AgentFailureMode? FailureMode { get; init; }
    public string? ErrorMessage { get; init; }
    public int LatencyMs { get; init; }
}

// What the domain wrapper adds around an Agent Framework agent: the session for this conversation,
// failure classification, and an AgentRun record with the model and token counts of the run.
public sealed class AgentRunner
{
    // Another run of the same conversation, agent and engineer can save the session first; this run's
    // turn is then added to the stored session and saved again. The save runs at most this many times in all.
    private const int MaxSaveAttempts = 3;

    private readonly IAgentSessionRepository _sessions;
    private readonly IAgentRunRepository _runs;
    private readonly ICorrelationContext _correlationContext;
    private readonly string _configuredModel;
    private readonly ILogger<AgentRunner> _logger;

    public AgentRunner(
        IAgentSessionRepository sessions,
        IAgentRunRepository runs,
        ICorrelationContext correlationContext,
        IChatClient chatClient,
        ILogger<AgentRunner> logger)
    {
        _sessions = sessions;
        _runs = runs;
        _correlationContext = correlationContext;
        _logger = logger;

        // The deployment from configuration, for runs whose response does not name its model.
        _configuredModel = chatClient.GetService<ChatClientMetadata>()?.DefaultModelId ?? string.Empty;
    }

    public async Task<AgentRunOutcome> RunAsync(AIAgent agent, AgentContext context, string message, IList<AITool> tools,
        ChatToolMode? toolMode, CancellationToken cancellationToken)
    {
        var name = agent.Name ?? throw new InvalidOperationException("Agents need a name to store their sessions.");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // The session this agent had in this conversation for this engineer, or a new one.
            var stored = await _sessions.GetAsync(context.ConversationId, name, context.RequesterId, cancellationToken);
            var session = await RestoreAsync(agent, stored, cancellationToken);
            var history = agent.GetService<ChatClientAgent>()?.ChatHistoryProvider as InMemoryChatHistoryProvider;
            var earlier = history?.GetMessages(session).Count ?? 0;

            // One agent serves every run (Step 5), so this run's tools travel in its run options. A restored session
            // already holds earlier tool results, so the caller can require a tool on the first model call;
            // FunctionInvokingChatClient lifts the requirement after that call, so the loop still ends.
            var options = new ChatClientAgentRunOptions(new ChatOptions { Tools = tools, ToolMode = toolMode });
            var response = await agent.RunAsync(message, session, options, cancellationToken);

            // A filtered answer arrives as a response that ends with the ContentFilter finish reason.
            // The session is not stored, so the next turn does not send the filtered exchange again.
            if (response.FinishReason == ChatFinishReason.ContentFilter)
            {
                _logger.LogWarning("{Agent} stopped by the content filter for conversation {ConversationId}", name, context.ConversationId);
                return new AgentRunOutcome
                {
                    FailureMode = AgentFailureMode.ContentSafety,
                    ErrorMessage = "The provider's content filter stopped the answer.",
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // The session now holds this turn's messages and tool calls; store it for the next turn.
            var turn = history?.GetMessages(session).Skip(earlier).ToList() ?? [];
            await SaveSessionAsync(agent, context, session, stored?.Version ?? 0, history, turn, cancellationToken);

            return new AgentRunOutcome { Response = response, LatencyMs = (int)stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            var mode = AgentFailureClassifier.Classify(ex);
            _logger.LogError(ex, "{Agent} failed for conversation {ConversationId} ({FailureMode})", name, context.ConversationId, mode);
            return new AgentRunOutcome { FailureMode = mode, ErrorMessage = ex.Message, LatencyMs = (int)stopwatch.ElapsedMilliseconds };
        }
    }

    // The model and token counts come from the response; a tool that calls a model adds its own.
    public Task LogRunAsync(AgentContext context, string agentName, AgentRunOutcome outcome,
        string? modelId = null, int toolInputTokens = 0, int toolOutputTokens = 0,
        bool isFallback = false, CancellationToken cancellationToken = default)
    {
        var usage = outcome.Response?.Usage;
        return _runs.LogAsync(new AgentRun
        {
            Id = Guid.NewGuid(),
            ConversationId = context.ConversationId,
            TicketId = context.CurrentTicket?.Id ?? Guid.Empty,
            CorrelationId = _correlationContext.CorrelationId,
            AgentName = agentName,
            InputTokens = (int)(usage?.InputTokenCount ?? 0) + toolInputTokens,
            OutputTokens = (int)(usage?.OutputTokenCount ?? 0) + toolOutputTokens,
            LatencyMs = outcome.LatencyMs,
            ModelId = string.IsNullOrEmpty(modelId) ? (outcome.Response?.RawRepresentation as ChatResponse)?.ModelId ?? _configuredModel : modelId,
            Timestamp = DateTime.UtcNow,
            IsFallback = isFallback || outcome.FailureMode is not null,
            ErrorMessage = outcome.ErrorMessage
        }, cancellationToken);
    }

    // A stored session that no longer deserializes, for example after a framework upgrade, starts over:
    // a conversation without its old history is better than one that fails on every turn.
    private async Task<AgentSession> RestoreAsync(AIAgent agent, AgentSessionRecord? stored, CancellationToken cancellationToken)
    {
        if (stored is null)
        {
            return await agent.CreateSessionAsync(cancellationToken);
        }

        try
        {
            return await agent.DeserializeSessionAsync(JsonSerializer.Deserialize<JsonElement>(stored.SessionJson), cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
            _logger.LogWarning(ex, "The stored {Agent} session of conversation {ConversationId} cannot be restored; it starts over",
                agent.Name, stored.ConversationId);
            return await agent.CreateSessionAsync(cancellationToken);
        }
    }

    // As for conversations in Chapter 6: the Version token makes a save fail when another run saved first.
    // This run's messages are then added to the stored session and saved again, without a second model call,
    // so the session keeps both turns.
    private async Task SaveSessionAsync(AIAgent agent, AgentContext context, AgentSession session, int version,
        InMemoryChatHistoryProvider? history, IReadOnlyList<ChatMessage> turn, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var state = await agent.SerializeSessionAsync(session, cancellationToken: cancellationToken);
            try
            {
                await _sessions.SaveAsync(new AgentSessionRecord
                {
                    ConversationId = context.ConversationId,
                    AgentName = agent.Name!,
                    RequesterId = context.RequesterId,
                    SessionJson = state.GetRawText(),
                    Version = version
                }, cancellationToken);
                return;
            }
            catch (AgentSessionConflictException) when (attempt < MaxSaveAttempts && history is not null)
            {
                var latest = await _sessions.GetAsync(context.ConversationId, agent.Name!, context.RequesterId, cancellationToken)
                    ?? throw new InvalidOperationException($"The {agent.Name} session disappeared during the run.");
                session = await RestoreAsync(agent, latest, cancellationToken);
                history.SetMessages(session, [.. history.GetMessages(session), .. turn]);
                version = latest.Version;
            }
        }
    }
}
