using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Validation;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Polly.Registry;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class ValidatedPromptExecutorTests
{
    private static readonly Dictionary<string, object> Variables = new()
    {
        ["subject"] = "Cannot sign in",
        ["description"] = "The account is locked.",
        ["productArea"] = "Authentication",
        ["customerTier"] = "Enterprise",
        ["priorityScore"] = "122.5",
        ["history"] = "Customer: The account is locked."
    };

    [Fact]
    public async Task ExecuteAsync_ThrowsWithDetails_WhenRequiredFieldsAreMissing()
    {
        const string answer = """{"summary":"Users are locked out.","reproductionSteps":"1. Reset the password.","recommendedPriority":"High"}""";
        var chatClient = ChatClientReturning(answer);
        var executor = new ValidatedPromptExecutor(chatClient.Object, Pipelines(), Options.Create(new AIProviderOptions()), Options.Create(new TokenPricingOptions()), NullLogger<ValidatedPromptExecutor>.Instance);

        var exception = await Assert.ThrowsAsync<SchemaValidationException>(() =>
            executor.ExecuteAsync(SupportPrompts.EscalationDraftExtraction, Variables, new EscalationDraftValidator()));

        Assert.Equal(answer, exception.RawOutput);
        Assert.Contains(exception.ValidationErrors, e => e.Contains("missing required properties") && e.Contains("'impact'"));
        chatClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTheDraft_WhenTheAnswerIsValid()
    {
        var chatClient = ChatClientReturning(
            """{"summary":"Users are locked out.","impact":"The team cannot work.","reproductionSteps":"1. Reset the password.","recommendedPriority":"High"}""");
        var executor = new ValidatedPromptExecutor(chatClient.Object, Pipelines(), Options.Create(new AIProviderOptions()), Options.Create(new TokenPricingOptions()), NullLogger<ValidatedPromptExecutor>.Instance);

        var draft = await executor.ExecuteAsync(SupportPrompts.EscalationDraftExtraction, Variables, new EscalationDraftValidator());

        Assert.Equal("High", draft.RecommendedPriority);
        Assert.Equal("The team cannot work.", draft.Impact);
    }

    private static Mock<IChatClient> ChatClientReturning(string text)
    {
        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)) { FinishReason = ChatFinishReason.Stop });
        return chatClient;
    }

    private static ResiliencePipelineRegistry<string> Pipelines()
    {
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(StructuredOutputPipeline.Name, (builder, _) =>
            builder.AddStructuredOutputResilience(new KernelResilienceOptions()));
        return registry;
    }
}
