using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class ActivityTests
{
    private readonly Mock<IEmbeddingClient> _embeddings = new();
    private readonly Mock<IVectorSearchService> _vectorSearch = new();
    private readonly Mock<ISupportContext> _supportContext = new();
    private readonly Mock<IConversationRepository> _conversations = new();
    private readonly Mock<IAuthorizationFilter> _authFilter = new();
    private readonly Mock<ISupportChatClient> _chatClient = new();
    private readonly SupportTicket _ticket = new() { Id = Guid.NewGuid(), Subject = "Locked out", ProductArea = "Authentication" };
    private readonly KnowledgeArticle _article = new() { Id = Guid.NewGuid(), Title = "Lockouts", ProductArea = "Authentication", IsPublished = true };

    public ActivityTests()
    {
        _embeddings
            .Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<EmbeddingOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddingResult { Vector = [1f, 0f], TokenCount = 4 });
        _vectorSearch
            .Setup(v => v.SearchAsync(It.IsAny<float[]>(), It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new VectorSearchResult
                {
                    Chunk = new ArticleChunk { Id = Guid.NewGuid(), ChunkIndex = 0, Content = "Accounts unlock after 30 minutes." },
                    Score = 0.7,
                    ArticleId = _article.Id,
                    ArticleTitle = _article.Title,
                    ProductArea = _article.ProductArea
                }
            ]);
        _supportContext
            .Setup(c => c.GetArticlesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_article]);
    }

    [Theory]
    [InlineData("Call +1 555 010 0199 today.", "Call [PHONE_REDACTED] today.")]
    [InlineData("Call (555) 010-0199 today.", "Call [PHONE_REDACTED] today.")]
    [InlineData("Write to ana [at] contoso [dot] com today.", "Write to [EMAIL_REDACTED] today.")]
    [InlineData("Write to ana at contoso dot com today.", "Write to [EMAIL_REDACTED] today.")]
    [InlineData("The endpoint allows 120 requests per minute.", "The endpoint allows 120 requests per minute.")]
    public void MaskSensitiveContent_MasksPhoneNumbersAndObfuscatedAddresses(string content, string expected)
    {
        Assert.Equal(expected, new SensitiveDataMasker().MaskSensitiveContent(content));
    }

    [Fact]
    public async Task CachingAuthorizationFilter_AsksTheInnerFilterOncePerArticle()
    {
        var other = _article with { Id = Guid.NewGuid() };
        _authFilter
            .Setup(f => f.CanAccessAsync(It.IsAny<string>(), It.IsAny<SupportTicket>(), It.IsAny<KnowledgeArticle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var filter = new CachingAuthorizationFilter(_authFilter.Object);

        await filter.CanAccessAsync("engineer-ana", _ticket, _article);
        await filter.CanAccessAsync("engineer-ana", _ticket, _article);
        await filter.CanAccessAsync("engineer-ana", _ticket, other);

        _authFilter.Verify(f => f.CanAccessAsync("engineer-ana", _ticket, _article, It.IsAny<CancellationToken>()), Times.Once);
        _authFilter.Verify(f => f.CanAccessAsync("engineer-ana", _ticket, other, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateGroundedResponseAsync_ReturnsTheFallback_WhenEveryArticleFailsAuthorization()
    {
        _authFilter
            .Setup(f => f.CanAccessAsync(It.IsAny<string>(), It.IsAny<SupportTicket>(), It.IsAny<KnowledgeArticle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateOrchestrator().GenerateGroundedResponseAsync(Request(), _ticket);

        Assert.False(result.IsGrounded);
        Assert.Equal(RagOrchestrator.NoContentResponse, result.Response);
        Assert.Empty(result.Citations);
        _chatClient.Verify(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<SupportChatOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateGroundedResponseAsync_SendsTheLastThreeTurns_OutsideTheKnowledgeBase()
    {
        string? prompt = null;
        _authFilter
            .Setup(f => f.CanAccessAsync(It.IsAny<string>(), It.IsAny<SupportTicket>(), It.IsAny<KnowledgeArticle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _conversations
            .Setup(r => r.GetByTicketAsync(_ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CopilotConversation
            {
                TicketId = _ticket.Id,
                Turns = Enumerable.Range(1, 4)
                    .Select(n => new ConversationTurn { TurnNumber = n, UserInput = $"question {n}", CopilotResponse = $"answer {n}" })
                    .ToList()
            });
        _chatClient
            .Setup(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<SupportChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback((string text, SupportChatOptions? _, CancellationToken _) => prompt = text)
            .ReturnsAsync(new SupportChatResponse { Content = $"Wait. [Article: Lockouts ({_article.Id})]" });

        await CreateOrchestrator().GenerateGroundedResponseAsync(Request(), _ticket);

        Assert.NotNull(prompt);
        var history = prompt[prompt.IndexOf("<conversation_history>", StringComparison.Ordinal)..];
        Assert.DoesNotContain("question 1", prompt);
        Assert.Contains("question 2", history);
        Assert.Contains("question 4", history);
        Assert.True(prompt.IndexOf("</knowledge_base>", StringComparison.Ordinal) < prompt.IndexOf("<conversation_history>", StringComparison.Ordinal));
    }

    private RagRequest Request() => new()
    {
        TicketId = _ticket.Id,
        Query = "locked out",
        ProductArea = "Authentication",
        RequesterId = "engineer-ana"
    };

    private RagOrchestrator CreateOrchestrator()
    {

        return new RagOrchestrator(
            _embeddings.Object,
            _vectorSearch.Object,
            _supportContext.Object,
            _conversations.Object,
            _authFilter.Object,
            new ContentSanitizer(new PromptInjectionDetector()),
            new SensitiveDataMasker(),
            new RagPromptBuilder(),
            _chatClient.Object,
            Options.Create(new VectorStoreOptions()),
            Options.Create(new RagOptions()),
            NullLogger<RagOrchestrator>.Instance);
    }
}
