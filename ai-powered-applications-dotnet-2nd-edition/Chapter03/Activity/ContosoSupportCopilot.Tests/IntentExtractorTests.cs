using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class IntentExtractorTests
{
    private const string Description = "The password reset email never arrived.";

    [Fact]
    public async Task ExtractAsync_UsesFallback_WhenConfidenceIsLow()
    {
        var chatClient = ChatClientReturning("""{"Intent": "Unclear", "Urgency": "Normal", "ProductArea": "Reporting", "ConfidenceScore": 0.4}""");
        var extractor = CreateExtractor(chatClient.Object, new KeywordFallbackExtractor());

        var intent = await extractor.ExtractAsync(Description);

        Assert.Equal("ManualReview", intent.Intent);
        Assert.Equal("Authentication", intent.ProductArea);
        Assert.Equal(0.0, intent.ConfidenceScore);
    }

    [Fact]
    public async Task ExtractAsync_KeepsAHigherUrgency_WhenItUsesTheFallback()
    {
        var chatClient = ChatClientReturning("""{"Intent": "Outage", "Urgency": "Critical", "ProductArea": "Reporting", "ConfidenceScore": 0.4}""");
        var extractor = CreateExtractor(chatClient.Object, new KeywordFallbackExtractor());

        var intent = await extractor.ExtractAsync(Description);

        Assert.Equal("ManualReview", intent.Intent);
        Assert.Equal("Critical", intent.Urgency);
        Assert.Equal("Authentication", intent.ProductArea);
    }

    [Fact]
    public async Task ExtractAsync_KeepsModelResult_WhenConfidenceIsHigh()
    {
        var chatClient = ChatClientReturning("""{"Intent": "PasswordReset", "Urgency": "High", "ProductArea": "Authentication", "ConfidenceScore": 0.9}""");
        var fallback = new Mock<IFallbackIntentExtractor>();
        var extractor = CreateExtractor(chatClient.Object, fallback.Object);

        var intent = await extractor.ExtractAsync(Description);

        Assert.Equal("PasswordReset", intent.Intent);
        Assert.Equal(0.9, intent.ConfidenceScore);
        fallback.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAsync_RequestsJsonOutput()
    {
        ChatOptions? sentOptions = null;
        var chatClient = ChatClientReturning("""{"Intent": "PasswordReset", "Urgency": "High", "ProductArea": "Authentication", "ConfidenceScore": 0.9}""",
            options => sentOptions = options);
        var extractor = CreateExtractor(chatClient.Object, fallback: null);

        await extractor.ExtractAsync(Description);

        Assert.IsType<ChatResponseFormatJson>(sentOptions?.ResponseFormat);
        Assert.Null(sentOptions?.Temperature);
    }

    [Fact]
    public async Task KeywordFallbackExtractor_MapsInvoiceToPayments()
    {
        var intent = await new KeywordFallbackExtractor().ExtractFallbackAsync("Where is my invoice?", CancellationToken.None);

        Assert.Equal("Payments", intent.ProductArea);
        Assert.Equal("ManualReview", intent.Intent);
    }

    [Theory]
    [InlineData("We need a rapid answer about the capital project", "General")]
    [InlineData("Our APIs return errors after the upgrade", "API")]
    public async Task KeywordFallbackExtractor_MatchesWholeWords(string description, string expectedArea)
    {
        var intent = await new KeywordFallbackExtractor().ExtractFallbackAsync(description, CancellationToken.None);

        Assert.Equal(expectedArea, intent.ProductArea);
    }

    private static Mock<IChatClient> ChatClientReturning(string json, Action<ChatOptions?>? captureOptions = null)
    {
        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((_, options, _) => captureOptions?.Invoke(options))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)) { FinishReason = ChatFinishReason.Stop });
        return chatClient;
    }

    private static IntentExtractor CreateExtractor(IChatClient chatClient, IFallbackIntentExtractor? fallback) =>
        new(chatClient, Options.Create(new TokenPricingOptions()), NullLogger<IntentExtractor>.Instance, fallback);
}
