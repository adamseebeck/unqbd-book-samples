using ContosoSupportCopilot.Domain.Validation;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Polly;
using Polly.Retry;

namespace ContosoSupportCopilot.Infrastructure.Resilience;

public static class StructuredOutputPipeline
{
    public const string Name = "structured-output";

    // Each model call inside already runs through the chat client's resilience pipeline from
    // Chapter 4: its timeouts, retries, and circuit breaker, which never sees invalid output.
    public static ResiliencePipelineBuilder AddStructuredOutputResilience(
        this ResiliencePipelineBuilder builder,
        KernelResilienceOptions options)
    {
        return builder
            // 1. One deadline for the whole structured call, validation retries included.
            .AddTimeout(TimeSpan.FromSeconds(options.TotalTimeoutSeconds))
            // 2. Ask again when the answer fails validation. A model that fails three times
            // in a row needs a better prompt, not more retries.
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<SchemaValidationException>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero
            });
    }
}
