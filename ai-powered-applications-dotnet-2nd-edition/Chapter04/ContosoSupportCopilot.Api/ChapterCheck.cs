using System.Diagnostics;
using System.Runtime.CompilerServices;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Polly.Registry;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this chapter's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var chat = provider.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();

        Console.WriteLine($"Contoso Support Copilot: Chapter 4 check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        if (chat?.ProviderName == "Offline")
        {
            Console.WriteLine("Offline provider: answers are local sample text and token counts are estimates.");
        }

        // Native plugin functions run in this process through the orchestrator.
        var orchestrator = provider.GetRequiredService<IKernelOrchestrator>();
        var score = await orchestrator.ExecutePluginFunctionAsync(
            "SupportTicketPlugin",
            "calculate_priority_score",
            new Dictionary<string, object> { ["ticketId"] = SampleData.PasswordTicketId });
        Console.WriteLine($"Priority score for the password ticket: {score}");

        var keywords = await orchestrator.ExecutePluginFunctionAsync(
            "SupportTicketPlugin",
            "extract_keywords",
            new Dictionary<string, object> { ["content"] = "Login fails with an error after the database upgrade" });
        Console.WriteLine($"Keywords: {keywords}");

        // A prompt template, a plugin result and structured output in one feature.
        var draft = await provider.GetRequiredService<EscalationDraftService>().GenerateDraftAsync(SampleData.PasswordTicketId);
        Console.WriteLine($"Escalation draft: {draft.RecommendedPriority} priority. {draft.Summary}");

        // Chapter 1's service sends its prompt through the same orchestrator and pipeline.
        var reply = await provider.GetRequiredService<SupportCopilotService>().DraftReplyAsync(SampleData.ApiTicketId);
        Console.WriteLine($"Semantic Kernel draft: {reply}");

        var resilient = await CheckResilienceAsync();
        if (string.IsNullOrWhiteSpace(draft.Summary) || string.IsNullOrWhiteSpace(reply) || !resilient)
        {
            Console.WriteLine("Check failed.");
            return 1;
        }

        Console.WriteLine("Check passed.");
        return 0;
    }

    // Runs prompts through the resilient chat client over a client that fails on purpose, with
    // short limits. It always uses the offline client, so it makes no requests to a real provider.
    private static async Task<bool> CheckResilienceAsync()
    {
        var options = new KernelResilienceOptions
        {
            MaxRetryAttempts = 2,
            BaseDelayMilliseconds = 100,
            MaxDelayMilliseconds = 200,
            AttemptTimeoutSeconds = 1,
            TotalTimeoutSeconds = 10,
            CircuitBreakerMinimumThroughput = 10
        };
        var variables = new Dictionary<string, object> { ["topic"] = "API rate limits" };
        const string Prompt = "Draft a reply to the customer about {{$topic}}.";

        // 1. Two connection failures, then an answer: the retry recovers.
        var flaky = new FaultInjectingChatClient(new OfflineChatClient(), failFirst: 2);
        var answer = await CreateOrchestrator(flaky, options).ExecutePromptAsync(Prompt, variables);
        Console.WriteLine($"Transient failures: {flaky.Attempts} attempts, answer: {answer}");

        // 2. A streamed reply whose first two starts fail: retried before any text reaches the caller.
        var flakyStream = new FaultInjectingChatClient(new OfflineChatClient(), failFirst: 2);
        var chunks = 0;
        await foreach (var update in CreateChatClient(flakyStream, options).GetStreamingResponseAsync("Draft a short reply."))
        {
            chunks += update.Text.Length > 0 ? 1 : 0;
        }

        Console.WriteLine($"Streaming with two failed starts: {flakyStream.Attempts} attempts, {chunks} chunks");

        // 3. Every attempt is too slow: the attempt timeout cancels each one, then the fallback answers.
        var slow = new FaultInjectingChatClient(new OfflineChatClient(), delay: TimeSpan.FromSeconds(5));
        var stopwatch = Stopwatch.StartNew();
        var timedOut = await CreateOrchestrator(slow, options with { MaxRetryAttempts = 1 }).ExecutePromptAsync(Prompt, variables);
        Console.WriteLine($"Slow responses: {slow.Attempts} attempts in {stopwatch.Elapsed.TotalSeconds:F1} s, fallback: {timedOut}");

        // 4. A service that keeps failing: the circuit opens, and later calls fail fast.
        var broken = new FaultInjectingChatClient(new OfflineChatClient(), failFirst: int.MaxValue);
        var breaking = CreateOrchestrator(broken, options with { CircuitBreakerMinimumThroughput = 2 });
        var first = await breaking.ExecutePromptAsync(Prompt, variables);
        Console.WriteLine($"Failing service: circuit opened after {broken.Attempts} attempts, fallback: {first}");
        var attemptsBefore = broken.Attempts;
        var second = await breaking.ExecutePromptAsync(Prompt, variables);
        Console.WriteLine($"While the circuit is open: {broken.Attempts - attemptsBefore} attempts, fallback: {second}");

        return flaky.Attempts == 3 && answer.StartsWith("[Offline sample")
            && flakyStream.Attempts == 3 && chunks > 1
            && slow.Attempts == 2 && timedOut.StartsWith("[Request timed out")
            && first.StartsWith("[AI service temporarily unavailable") && broken.Attempts == attemptsBefore;
    }

    // The same composition as the application: the resilient client in front of the provider's client.
    private static ResilientChatClient CreateChatClient(IChatClient innerClient, KernelResilienceOptions options)
    {
        var pipelines = new ResiliencePipelineRegistry<string>();
        pipelines.TryAddBuilder(KernelResiliencePipeline.Name, (builder, _) => builder.AddKernelResilience(options));
        return new ResilientChatClient(
            innerClient,
            pipelines.GetPipeline(KernelResiliencePipeline.Name),
            TimeSpan.FromSeconds(options.TotalTimeoutSeconds));
    }

    private static SemanticKernelOrchestrator CreateOrchestrator(IChatClient innerClient, KernelResilienceOptions options)
    {
        IChatClient chatClient = CreateChatClient(innerClient, options);
        var kernel = new Kernel(new ServiceCollection().AddSingleton(chatClient).BuildServiceProvider());
        return new SemanticKernelOrchestrator(kernel, NullLogger<SemanticKernelOrchestrator>.Instance);
    }

    // Fails or slows down calls on purpose, then passes requests to the inner client.
    private sealed class FaultInjectingChatClient(IChatClient innerClient, int failFirst = 0, TimeSpan? delay = null)
        : DelegatingChatClient(innerClient)
    {
        public int Attempts { get; private set; }

        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await FailOrWaitAsync(cancellationToken);
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }

        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await FailOrWaitAsync(cancellationToken);
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                yield return update;
            }
        }

        private async Task FailOrWaitAsync(CancellationToken cancellationToken)
        {
            Attempts++;
            if (delay is { } wait)
            {
                await Task.Delay(wait, cancellationToken);
            }

            if (Attempts <= failFirst)
            {
                throw new HttpRequestException($"Simulated connection failure {Attempts}");
            }
        }
    }
}
