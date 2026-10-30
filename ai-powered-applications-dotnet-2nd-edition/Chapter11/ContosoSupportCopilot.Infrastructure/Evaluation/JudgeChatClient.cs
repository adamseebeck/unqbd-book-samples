using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.Evaluation;

// The chat client that the LLM judges call. The quality evaluators send Temperature 0, TopP 1, and zero
// presence and frequency penalties, which most reasoning deployments reject, so this client removes them.
// Reasoning tokens count against the output limit, so it also raises the evaluators' own limits (800
// tokens, 1,600 for RetrievalEvaluator), and a judgment that still stops at the limit fails loudly.
public sealed class JudgeChatClient : DelegatingChatClient
{
    private readonly int _maxOutputTokens;

    public JudgeChatClient(IChatClient innerClient, int maxOutputTokens)
        : base(innerClient)
    {
        _maxOutputTokens = maxOutputTokens;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, ForJudge(options), cancellationToken);
        if (response.FinishReason == ChatFinishReason.Length)
        {
            throw new InvalidOperationException(
                $"The judge stopped at its limit of {_maxOutputTokens} output tokens before it finished. Raise Evaluation:JudgeMaxOutputTokens.");
        }

        return response;
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(messages, ForJudge(options), cancellationToken);

    // A copy, because each evaluator reuses one ChatOptions instance for all of its calls.
    private ChatOptions ForJudge(ChatOptions? options)
    {
        var judgeOptions = options?.Clone() ?? new ChatOptions();
        judgeOptions.Temperature = null;
        judgeOptions.TopP = null;
        judgeOptions.PresencePenalty = null;
        judgeOptions.FrequencyPenalty = null;
        judgeOptions.MaxOutputTokens = Math.Max(judgeOptions.MaxOutputTokens ?? 0, _maxOutputTokens);
        return judgeOptions;
    }
}
