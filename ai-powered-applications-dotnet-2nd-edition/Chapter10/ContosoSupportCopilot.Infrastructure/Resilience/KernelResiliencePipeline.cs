using System.ClientModel;
using System.Globalization;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace ContosoSupportCopilot.Infrastructure.Resilience;

public static class KernelResiliencePipeline
{
    public const string Name = "kernel";

    // Polly runs strategies in the order you add them: the first one added is the outermost.
    public static ResiliencePipelineBuilder AddKernelResilience(
        this ResiliencePipelineBuilder builder,
        KernelResilienceOptions options)
    {
        return builder
            // 1. One deadline for the whole call, retries and their delays included.
            .AddTimeout(TimeSpan.FromSeconds(options.TotalTimeoutSeconds))
            // 2. The only retry layer: AIClientFactory turns the OpenAI SDK's own retries off.
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = args => ValueTask.FromResult(IsTransient(args.Outcome.Exception, args.Context.CancellationToken)),
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(options.BaseDelayMilliseconds),
                MaxDelay = TimeSpan.FromMilliseconds(options.MaxDelayMilliseconds),
                // A throttled service says how long to wait; null keeps the backoff delay.
                DelayGenerator = args => ValueTask.FromResult(RetryAfter(args.Outcome.Exception))
            })
            // 3. Stop calling a service that keeps failing. Every attempt counts: an outcome that
            //    IsTransient rejects, the caller's cancellation included, counts as a success.
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = args => ValueTask.FromResult(IsTransient(args.Outcome.Exception, args.Context.CancellationToken)),
                FailureRatio = options.CircuitBreakerFailureRatio,
                MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds)
            })
            // 4. A time limit for each attempt.
            .AddTimeout(TimeSpan.FromSeconds(options.AttemptTimeoutSeconds));
    }

    // The failures the OpenAI SDK would otherwise retry (no response at all, 408, 429, 500, 502, 503,
    // 504, and its own network timeout), connection failures of clients such as Ollama's, and the
    // attempt timeout. The caller's cancellation is never one of them.
    public static bool IsTransient(Exception? exception, CancellationToken cancellationToken) => exception switch
    {
        TimeoutRejectedException or HttpRequestException or IOException => true,
        ClientResultException failure => failure.Status is 0 or 408 or 429 or 500 or 502 or 503 or 504,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false
    };

    // A throttled service names its wait in retry-after-ms or in Retry-After (seconds).
    private static TimeSpan? RetryAfter(Exception? exception)
    {
        var headers = (exception as ClientResultException)?.GetRawResponse()?.Headers;
        if (headers is null)
        {
            return null;
        }

        if (headers.TryGetValue("retry-after-ms", out var milliseconds)
            && double.TryParse(milliseconds, NumberStyles.Float, CultureInfo.InvariantCulture, out var wait))
        {
            return TimeSpan.FromMilliseconds(wait);
        }

        return headers.TryGetValue("Retry-After", out var seconds)
            && int.TryParse(seconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delay)
            ? TimeSpan.FromSeconds(delay)
            : null;
    }
}
