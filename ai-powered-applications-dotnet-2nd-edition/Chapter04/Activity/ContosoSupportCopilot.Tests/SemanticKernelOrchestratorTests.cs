using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Moq;
using Polly.Registry;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class SemanticKernelOrchestratorTests
{
    private const string Prompt = "Draft a reply about {{$topic}}.";

    private static readonly KernelResilienceOptions FastOptions = new()
    {
        MaxRetryAttempts = 1,
        BaseDelayMilliseconds = 1,
        MaxDelayMilliseconds = 1,
        AttemptTimeoutSeconds = 1,
        TotalTimeoutSeconds = 10,
        CircuitBreakerMinimumThroughput = 100
    };

    [Fact]
    public async Task ExecutePromptAsync_IncludesExceptionType_WhenDetailedFallbacksAreEnabled()
    {
        var chatClient = ChatClient();
        chatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(async (IEnumerable<ChatMessage> _, ChatOptions? _, CancellationToken token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "too late"));
            });
        var orchestrator = CreateOrchestrator(chatClient.Object, FastOptions with { EnableDetailedFallbacks = true });

        var result = await orchestrator.ExecutePromptAsync(Prompt, Variables());

        Assert.Equal("[Request timed out. The AI service is experiencing high latency.] (TimeoutException)", result);
    }

    [Fact]
    public async Task ExecutePromptAsync_ReturnsGenericMessage_WhenDetailedFallbacksAreOff()
    {
        var chatClient = ChatClient();
        chatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The deployment rejected the request."));
        var orchestrator = CreateOrchestrator(chatClient.Object, FastOptions);

        var result = await orchestrator.ExecutePromptAsync(Prompt, Variables());

        Assert.Equal("[An error occurred while processing your request.]", result);
    }

    [Fact]
    public async Task ExecutePromptAsync_FailsFast_WhileTheCircuitIsOpen()
    {
        var chatClient = ChatClient();
        chatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var orchestrator = CreateOrchestrator(chatClient.Object, FastOptions with { CircuitBreakerMinimumThroughput = 2 });

        await orchestrator.ExecutePromptAsync(Prompt, Variables());
        var result = await orchestrator.ExecutePromptAsync(Prompt, Variables());

        Assert.Equal("[AI service temporarily unavailable. Please try again later.]", result);
        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static Dictionary<string, object> Variables() => new() { ["topic"] = "API rate limits" };

    // Semantic Kernel asks the chat client for its metadata before it sends a prompt.
    private static Mock<IChatClient> ChatClient()
    {
        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(c => c.GetService(typeof(ChatClientMetadata), null))
            .Returns(new ChatClientMetadata("Test", defaultModelId: "test-model"));
        return chatClient;
    }

    // The application's composition: the resilient chat client in front of the model's client.
    private static SemanticKernelOrchestrator CreateOrchestrator(IChatClient innerClient, KernelResilienceOptions options)
    {
        var pipelines = new ResiliencePipelineRegistry<string>();
        pipelines.TryAddBuilder(KernelResiliencePipeline.Name, (builder, _) => builder.AddKernelResilience(options));
        IChatClient chatClient = new ResilientChatClient(
            innerClient,
            pipelines.GetPipeline(KernelResiliencePipeline.Name),
            TimeSpan.FromSeconds(options.TotalTimeoutSeconds));
        var kernel = new Kernel(new ServiceCollection().AddSingleton(chatClient).BuildServiceProvider());
        return new SemanticKernelOrchestrator(kernel, Options.Create(options), NullLogger<SemanticKernelOrchestrator>.Instance);
    }
}
