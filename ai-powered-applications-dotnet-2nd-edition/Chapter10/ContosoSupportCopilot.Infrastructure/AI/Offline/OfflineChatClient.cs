using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>
/// An <see cref="IChatClient"/> that answers locally with deterministic sample output,
/// so the companion code runs without an AI account, network access or a GPU.
/// The same request always produces the same response. Every answer is marked as an
/// offline sample: it demonstrates the application's flow, not the quality of a model.
/// </summary>
public sealed class OfflineChatClient : IChatClient
{
    public const string ProviderName = "Offline";
    public const string ModelName = "offline-sample";

    private static readonly ChatClientMetadata Metadata = new(ProviderName, defaultModelId: ModelName);
    private readonly IReadOnlyList<IOfflineResponder> _responders;

    /// <param name="responders">Optional responders that can answer specific requests
    /// before the built-in sample responses are used.</param>
    public OfflineChatClient(IEnumerable<IOfflineResponder>? responders = null)
    {
        _responders = responders?.ToArray() ?? [];
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = OfflineChatRequest.Create(messages, options);
        var text = OfflineResponder.CreateResponse(request, _responders);

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            ModelId = ModelName,
            ResponseId = request.Fingerprint,
            FinishReason = ChatFinishReason.Stop,
            Usage = OfflineResponder.EstimateUsage(request, text)
        };

        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);

        foreach (var piece in OfflineResponder.SplitForStreaming(response.Text))
        {
            // A short pause per piece makes streaming visible in a console or browser.
            await Task.Delay(10, cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, piece)
            {
                ModelId = ModelName,
                ResponseId = response.ResponseId
            };
        }

        // Like real providers, the last update carries the token usage for the whole response.
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(response.Usage!)])
        {
            ModelId = ModelName,
            ResponseId = response.ResponseId,
            FinishReason = ChatFinishReason.Stop
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(ChatClientMetadata))
        {
            return Metadata;
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }
}
