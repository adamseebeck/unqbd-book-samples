using System.Runtime.CompilerServices;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class SupportChatClient : ISupportChatClient
{
    private readonly IChatClient _chatClient;

    public SupportChatClient(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<SupportChatResponse> CompleteAsync(string prompt, SupportChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ChatResponse response = await _chatClient.GetResponseAsync(prompt, ToChatOptions(options), cancellationToken);

        return new SupportChatResponse
        {
            Content = response.Text,
            InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
            ModelId = response.ModelId ?? string.Empty,
            FinishReason = response.FinishReason?.Value ?? string.Empty
        };
    }

    public async IAsyncEnumerable<SupportChatChunk> CompleteStreamingAsync(
        string prompt,
        SupportChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in _chatClient.GetStreamingResponseAsync(prompt, ToChatOptions(options), cancellationToken))
        {
            updates.Add(update);
            if (update.Text.Length > 0)
            {
                yield return new SupportChatChunk { Content = update.Text };
            }
        }

        // Providers report usage in the final updates; ToChatResponse collects it.
        UsageDetails? usage = updates.ToChatResponse().Usage;
        yield return new SupportChatChunk
        {
            IsComplete = true,
            InputTokens = (int)(usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(usage?.OutputTokenCount ?? 0)
        };
    }

    private static ChatOptions? ToChatOptions(SupportChatOptions? options) => options is null
        ? null
        : new ChatOptions
        {
            Instructions = options.SystemPrompt,
            MaxOutputTokens = options.MaxOutputTokens,
            Temperature = options.Temperature
        };
}
