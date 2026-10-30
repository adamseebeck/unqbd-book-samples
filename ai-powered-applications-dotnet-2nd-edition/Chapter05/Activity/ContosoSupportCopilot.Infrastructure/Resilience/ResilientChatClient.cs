using System.Diagnostics;
using System.Runtime.CompilerServices;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace ContosoSupportCopilot.Infrastructure.Resilience;

// Chat middleware that runs every model call through the resilience pipeline. Polly's exceptions
// stay inside the Infrastructure project: callers see TimeoutException when a time limit runs out
// and InvalidOperationException, wrapping BrokenCircuitException, while the circuit is open.
public sealed class ResilientChatClient : DelegatingChatClient
{
    private readonly ResiliencePipeline _pipeline;
    private readonly TimeSpan _totalTimeout;

    public ResilientChatClient(IChatClient innerClient, ResiliencePipeline pipeline, TimeSpan totalTimeout)
        : base(innerClient)
    {
        _pipeline = pipeline;
        _totalTimeout = totalTimeout;
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // A retry sends the same messages again, so read them once.
        var history = messages.ToList();
        return RunAsync(async token => await base.GetResponseAsync(history, options, token), cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = messages.ToList();
        var started = Stopwatch.GetTimestamp();
        CancellationTokenSource? request = null;
        IAsyncEnumerator<ChatResponseUpdate>? updates = null;

        try
        {
            // An attempt opens the stream and waits for its first update, so a failed or slow
            // start is retried, timed, and counted by the circuit breaker like a complete call.
            var hasUpdate = await RunAsync(async attemptToken =>
            {
                await ReleaseAsync(updates, request);
                request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                updates = base.GetStreamingResponseAsync(history, options, request.Token).GetAsyncEnumerator();
                using var attempt = attemptToken.Register(request.Cancel);
                return await updates.MoveNextAsync();
            }, cancellationToken);

            // The rest of the stream is not retried, because the caller has already shown the
            // text before it, and it must end within what is left of the total timeout.
            var remaining = _totalTimeout - Stopwatch.GetElapsedTime(started);
            request!.CancelAfter(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
            while (hasUpdate)
            {
                yield return updates!.Current;
                hasUpdate = await ReadNextAsync(updates, request, cancellationToken);
            }
        }
        finally
        {
            await ReleaseAsync(updates, request);
        }
    }

    private async Task<T> RunAsync<T>(Func<CancellationToken, ValueTask<T>> callback, CancellationToken cancellationToken)
    {
        try
        {
            return await _pipeline.ExecuteAsync(callback, cancellationToken);
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
    }

    private static async Task<bool> ReadNextAsync(
        IAsyncEnumerator<ChatResponseUpdate> updates,
        CancellationTokenSource request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await updates.MoveNextAsync();
        }
        catch (OperationCanceledException ex) when (request.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The streamed answer did not finish within the total timeout.", ex);
        }
    }

    private static async ValueTask ReleaseAsync(IAsyncEnumerator<ChatResponseUpdate>? updates, CancellationTokenSource? request)
    {
        if (updates is not null)
        {
            await updates.DisposeAsync();
        }

        request?.Dispose();
    }
}

public static class ResilientChatClientExtensions
{
    // Puts the "kernel" pipeline in front of the chat client, so every model call runs through it.
    public static ChatClientBuilder UseKernelResilience(this ChatClientBuilder builder) =>
        builder.Use((innerClient, services) =>
        {
            var options = services.GetRequiredService<IOptions<KernelResilienceOptions>>().Value;
            var pipeline = services.GetRequiredService<ResiliencePipelineProvider<string>>()
                .GetPipeline(KernelResiliencePipeline.Name);
            return new ResilientChatClient(innerClient, pipeline, TimeSpan.FromSeconds(options.TotalTimeoutSeconds));
        });
}
