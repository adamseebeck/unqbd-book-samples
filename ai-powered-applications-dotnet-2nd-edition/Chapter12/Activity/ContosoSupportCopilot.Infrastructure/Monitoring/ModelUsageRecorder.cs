using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContosoSupportCopilot.Infrastructure.Monitoring;

// Stores the usage of each model call in the copilot database, in a scope of its own. A failed write is
// logged and dropped: cost accounting must never fail the call it accounts for.
public sealed class ModelUsageRecorder
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ICorrelationContext _correlation;
    private readonly ILogger<ModelUsageRecorder> _logger;

    public ModelUsageRecorder(IServiceScopeFactory scopes, ICorrelationContext correlation, ILogger<ModelUsageRecorder> logger)
    {
        _scopes = scopes;
        _correlation = correlation;
        _logger = logger;
    }

    public async Task RecordAsync(string operation, string? modelId, UsageDetails? usage, CancellationToken cancellationToken = default)
    {
        if (usage is null)
        {
            return;
        }

        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CopilotDbContext>();
            db.ModelUsage.Add(new ModelUsageRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Operation = operation,
                ModelId = modelId ?? string.Empty,
                InputTokens = usage.InputTokenCount ?? 0,
                CachedInputTokens = usage.CachedInputTokenCount ?? 0,
                OutputTokens = usage.OutputTokenCount ?? 0,
                CorrelationId = _correlation.CorrelationId
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not record the usage of a {Operation} call", operation);
        }
    }
}

// Records the usage that every chat response reports after the call returns, and a stream's usage once the caller
// has read it to the end. The write ignores the caller's cancellation: once the provider answered, the tokens are spent.
public sealed class UsageRecordingChatClient : DelegatingChatClient
{
    private readonly ModelUsageRecorder _recorder;

    public UsageRecordingChatClient(IChatClient innerClient, ModelUsageRecorder recorder)
        : base(innerClient)
    {
        _recorder = recorder;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);
        await _recorder.RecordAsync(ModelUsageRecord.Chat, response.ModelId, response.Usage, CancellationToken.None);
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            updates.Add(update);
            yield return update;
        }

        // Providers report usage in the final updates; ToChatResponse collects it. A caller that stops reading
        // early never reaches this line, so that stream's usage is not recorded.
        var response = updates.ToChatResponse();
        await _recorder.RecordAsync(ModelUsageRecord.Chat, response.ModelId, response.Usage, CancellationToken.None);
    }
}

// The same for embedding requests, which bill input tokens at their own rate.
public sealed class UsageRecordingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly ModelUsageRecorder _recorder;

    public UsageRecordingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator, ModelUsageRecorder recorder)
        : base(innerGenerator)
    {
        _recorder = recorder;
    }

    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var embeddings = await base.GenerateAsync(values, options, cancellationToken);
        await _recorder.RecordAsync(ModelUsageRecord.Embeddings, embeddings.FirstOrDefault()?.ModelId, embeddings.Usage, CancellationToken.None);
        return embeddings;
    }
}
