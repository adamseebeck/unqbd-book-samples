using System.Diagnostics;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// Drafting is one fixed model call that IDraftingService already makes, with Chapter 6's fallback,
// so this agent calls its tool directly. An agent loop would add a model round trip and no decision.
public class DraftingAgent : IAgent
{
    private static readonly DraftingOptions Options = new() { MaxOutputTokens = 2000 };

    private readonly AgentToolRegistry _tools;
    private readonly AgentRunner _runner;
    private readonly ICorrelationContext _correlationContext;

    public DraftingAgent(AgentToolRegistry tools, AgentRunner runner, ICorrelationContext correlationContext)
    {
        _tools = tools;
        _runner = runner;
        _correlationContext = correlationContext;
    }

    public string Name => AgentNames.Drafting;

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var ticket = context.CurrentTicket ?? throw new InvalidOperationException("DraftingAgent requires a current ticket.");
        // With the knowledge agent's answer in the context, the draft takes its facts from the cited articles.
        var toolContext = new AgentToolContext(_correlationContext.CorrelationId, context.ConversationId, null, ticket,
            context.RequesterId, context.Knowledge);

        var stopwatch = Stopwatch.StartNew();
        var result = await _tools.Resolve(DraftingTool.ToolName).ExecuteAsync(Options, toolContext, cancellationToken);
        var outcome = new AgentRunOutcome
        {
            LatencyMs = (int)stopwatch.ElapsedMilliseconds,
            FailureMode = result.IsSuccess ? null : result.FailureMode,
            ErrorMessage = result.ErrorMessage
        };

        if (result.Value is not DraftReply reply)
        {
            await _runner.LogRunAsync(context, Name, outcome, cancellationToken: cancellationToken);
            return AgentResults.Failure(Name, result.FailureMode);
        }

        // The model and token counts come from the reply. A fallback reply goes to a person first.
        await _runner.LogRunAsync(context, Name, outcome, reply.ModelId, reply.InputTokens, reply.OutputTokens,
            reply.IsFallback, cancellationToken);

        return new AgentResult
        {
            Response = reply.Text,
            RequiresHandoff = reply.IsFallback,
            NextAgent = reply.IsFallback ? AgentNames.Escalation : null,
            Artifacts = new Dictionary<string, object>
            {
                [AgentArtifactKeys.Draft] = reply.Text,
                [AgentArtifactKeys.IsFallback] = reply.IsFallback,
                [AgentArtifactKeys.ModelId] = reply.ModelId
            }
        };
    }
}
