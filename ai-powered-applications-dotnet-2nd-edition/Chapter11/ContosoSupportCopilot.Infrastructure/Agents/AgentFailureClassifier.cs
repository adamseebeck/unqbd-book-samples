using System.ClientModel;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// Maps an exception from a model call or a tool to the recovery the caller should choose.
public static class AgentFailureClassifier
{
    public static AgentFailureMode Classify(Exception exception) => exception switch
    {
        // Worth another attempt later: timeouts, an open circuit, throttling, service errors, a connection that
        // never answered (status 0), and a session that other runs kept saving first.
        TimeoutRejectedException or TimeoutException or BrokenCircuitException or HttpRequestException => AgentFailureMode.Transient,
        InvalidOperationException { InnerException: BrokenCircuitException or TimeoutRejectedException } => AgentFailureMode.Transient,
        ClientResultException { Status: 0 or 408 or 429 or >= 500 } => AgentFailureMode.Transient,
        AgentSessionConflictException => AgentFailureMode.Transient,

        // A person must act: retrying cannot grant access.
        UnauthorizedAccessException => AgentFailureMode.Authorization,
        ClientResultException { Status: 401 or 403 } => AgentFailureMode.Authorization,

        // Azure OpenAI rejects a filtered prompt with status 400 and the code content_filter. A filtered
        // answer is not an exception: AgentRunner checks the response's finish reason for it.
        ClientResultException { Status: 400 } ex when ex.Message.Contains("content_filter", StringComparison.OrdinalIgnoreCase)
            => AgentFailureMode.ContentSafety,

        AggregateException { InnerExceptions.Count: > 0 } aggregate => Classify(aggregate.InnerExceptions[0]),
        _ => AgentFailureMode.Unrecoverable
    };
}
