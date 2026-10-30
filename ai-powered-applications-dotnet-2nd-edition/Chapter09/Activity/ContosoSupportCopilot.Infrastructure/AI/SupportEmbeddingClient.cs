using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace ContosoSupportCopilot.Infrastructure.AI;

public sealed class SupportEmbeddingClient : IEmbeddingClient
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly ResiliencePipeline _pipeline;
    private readonly int? _dimensions;

    public SupportEmbeddingClient(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        ResiliencePipelineProvider<string> pipelines,
        IOptions<VectorStoreOptions> options)
    {
        _generator = generator;
        _pipeline = pipelines.GetPipeline(KernelResiliencePipeline.Name);
        _dimensions = options.Value.Dimensions;
    }

    public async Task<EmbeddingResult> GenerateAsync(
        string text,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        // Stored vectors and query vectors must have the same length, so both use one setting.
        var generationOptions = new EmbeddingGenerationOptions { Dimensions = options?.Dimensions ?? _dimensions };

        // Embedding calls do not pass the chat client's middleware, so this client runs the Chapter 4 pipeline
        // itself. Like that middleware, it reports a timeout as TimeoutException and an open circuit as
        // InvalidOperationException, so no Polly type reaches the callers of IEmbeddingClient.
        GeneratedEmbeddings<Embedding<float>> generated;
        try
        {
            generated = await _pipeline.ExecuteAsync(
                async token => await _generator.GenerateAsync([text], generationOptions, token),
                cancellationToken);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new TimeoutException("The AI service did not answer within the configured time limits.", ex);
        }
        catch (BrokenCircuitException ex)
        {
            throw new InvalidOperationException("The AI service is temporarily unavailable: too many recent calls failed.", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // The client's own network timeout (AI:TimeoutSeconds) ended the last attempt.
            throw new TimeoutException("The AI service did not answer within the configured time limits.", ex);
        }

        var embedding = generated[0];
        return new EmbeddingResult
        {
            Vector = embedding.Vector.ToArray(),
            TokenCount = (int)(generated.Usage?.InputTokenCount ?? 0),
            ModelId = embedding.ModelId
                ?? _generator.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId
                ?? string.Empty
        };
    }
}
