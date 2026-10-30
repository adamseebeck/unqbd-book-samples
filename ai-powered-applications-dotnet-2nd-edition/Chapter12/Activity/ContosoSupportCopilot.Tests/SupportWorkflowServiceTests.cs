using System.Security;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class SupportWorkflowServiceTests
{
    private readonly Mock<IIntentExtractor> _intentExtractor = new();
    private readonly Mock<IDraftingService> _draftingService = new();
    private readonly Mock<ISupportChatClient> _chatClient = new();
    private readonly Mock<ISupportContext> _supportContext = new();
    private readonly Mock<ICorrelationContext> _correlationContext = new();
    private readonly Mock<IConversationRepository> _conversationRepository = new();
    private readonly Mock<IAgentRunRepository> _agentRunRepository = new();
    private readonly SupportTicket _ticket;
    private readonly SupportWorkflowService _service;

    public SupportWorkflowServiceTests()
    {
        _ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            Subject = "Cannot sign in",
            Description = "Every sign in attempt says the account is locked.",
            ProductArea = "Authentication",
            CreatedAt = DateTime.UtcNow
        };

        _correlationContext.Setup(c => c.CorrelationId).Returns("test-correlation-id");
        _supportContext.Setup(c => c.GetTicketAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
        _intentExtractor
            .Setup(i => i.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportIntent { Intent = "PasswordReset", Urgency = "High", ProductArea = "Authentication", ConfidenceScore = 0.9 });

        _service = new SupportWorkflowService(
            _intentExtractor.Object,
            _draftingService.Object,
            _chatClient.Object,
            _supportContext.Object,
            _correlationContext.Object,
            _conversationRepository.Object,
            _agentRunRepository.Object);
    }

    [Fact]
    public async Task ProcessTicketAsync_CreatesConversation_AndRecordsTheRun()
    {
        _conversationRepository
            .Setup(r => r.GetByTicketAsync(_ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CopilotConversation?)null);
        _draftingService
            .Setup(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftReply { Text = "Please sign out of every device.", ModelId = "test-model", InputTokens = 120, OutputTokens = 45 });

        var result = await _service.ProcessTicketAsync(_ticket.Id, "Help needed");

        Assert.NotEqual(Guid.Empty, result.ConversationId);
        Assert.Equal("Please sign out of every device.", result.Response);
        Assert.Equal("test-correlation-id", result.CorrelationId);
        _conversationRepository.Verify(r => r.SaveAsync(
            It.Is<CopilotConversation>(c => c.TicketId == _ticket.Id && c.Turns.Count == 1 && c.CurrentTurn == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        _agentRunRepository.Verify(r => r.LogAsync(
            It.Is<AgentRun>(a => a.CorrelationId == "test-correlation-id" && a.AgentName == "DraftingAgent" && a.TicketId == _ticket.Id
                && a.ModelId == "test-model" && a.InputTokens == 120 && a.OutputTokens == 45 && !a.IsFallback),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_AddsATurn_ToTheActiveConversation()
    {
        var existing = new CopilotConversation
        {
            Id = Guid.NewGuid(),
            TicketId = _ticket.Id,
            CurrentTurn = 1,
            Turns = [new ConversationTurn { TurnNumber = 1, UserInput = "First question", CopilotResponse = "First answer" }]
        };
        _conversationRepository.Setup(r => r.GetByTicketAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _draftingService
            .Setup(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftReply { Text = "Second answer" });

        var result = await _service.ProcessTicketAsync(_ticket.Id, "Second question");

        Assert.Equal(existing.Id, result.ConversationId);
        Assert.Single(existing.Turns);
        _conversationRepository.Verify(r => r.SaveAsync(
            It.Is<CopilotConversation>(c => c.Id == existing.Id && c.CurrentTurn == 2 && c.Turns.Count == 2 && c.Turns[1].UserInput == "Second question"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_AddsTheTurnAgain_WhenAnotherRequestSavedFirst()
    {
        var turnOne = new ConversationTurn { TurnNumber = 1, UserInput = "First question" };
        var loaded = new CopilotConversation { Id = Guid.NewGuid(), TicketId = _ticket.Id, CurrentTurn = 1, Turns = [turnOne] };
        var savedMeanwhile = loaded with { CurrentTurn = 2, Turns = [turnOne, new ConversationTurn { TurnNumber = 2, UserInput = "Other request" }] };
        _conversationRepository
            .SetupSequence(r => r.GetByTicketAsync(_ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loaded)
            .ReturnsAsync(savedMeanwhile);
        _conversationRepository
            .SetupSequence(r => r.SaveAsync(It.IsAny<CopilotConversation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConversationConflictException(_ticket.Id, new InvalidOperationException("Stale turn count")))
            .Returns(Task.CompletedTask);
        _draftingService
            .Setup(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftReply { Text = "Third answer" });

        var result = await _service.ProcessTicketAsync(_ticket.Id, "Third question");

        Assert.Equal(loaded.Id, result.ConversationId);
        _draftingService.Verify(
            d => d.DraftReplyAsync(It.IsAny<SupportTicket>(), It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _conversationRepository.Verify(r => r.SaveAsync(
            It.Is<CopilotConversation>(c => c.CurrentTurn == 3 && c.Turns.Count == 3 && c.Turns[2].UserInput == "Third question"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_RecordsTheFailure_WhenDraftingThrows()
    {
        _conversationRepository
            .Setup(r => r.GetByTicketAsync(_ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CopilotConversation?)null);
        _draftingService
            .Setup(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _service.ProcessTicketAsync(_ticket.Id, "Help needed");

        Assert.Equal("I'm experiencing technical difficulties. Please try again shortly.", result.Response);
        _conversationRepository.Verify(r => r.SaveAsync(It.Is<CopilotConversation>(c => c.Turns.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _agentRunRepository.Verify(r => r.LogAsync(
            It.Is<AgentRun>(a => a.IsFallback && a.ErrorMessage == "Connection refused"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_StillAnswers_WhenTheAIServiceIsDown()
    {
        _conversationRepository
            .Setup(r => r.GetByTicketAsync(_ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CopilotConversation?)null);
        _intentExtractor
            .Setup(i => i.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The AI service is temporarily unavailable"));
        _draftingService
            .Setup(d => d.DraftReplyAsync(_ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The AI service is temporarily unavailable"));

        var result = await _service.ProcessTicketAsync(_ticket.Id, "Help needed");

        Assert.Equal("I'm experiencing technical difficulties. Please try again shortly.", result.Response);
        Assert.Equal("ManualReview", result.Intent.Intent);
        Assert.Equal(_ticket.Priority, result.Intent.Urgency);
        _conversationRepository.Verify(r => r.SaveAsync(It.Is<CopilotConversation>(c => c.Turns.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _agentRunRepository.Verify(r => r.LogAsync(
            It.Is<AgentRun>(a => a.IsFallback && a.TicketId == _ticket.Id && a.ErrorMessage!.StartsWith("Intent extraction failed")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_HandsOff_LowConfidencePaymentIssues()
    {
        _intentExtractor
            .Setup(i => i.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportIntent { Intent = "PaymentIssue", Urgency = "Normal", ProductArea = "Payments", ConfidenceScore = 0.6 });

        var result = await _service.ProcessTicketAsync(_ticket.Id, "Something about my card");

        Assert.Contains("payments specialist", result.Response);
        _draftingService.Verify(
            d => d.DraftReplyAsync(It.IsAny<SupportTicket>(), It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _agentRunRepository.Verify(r => r.LogAsync(
            It.Is<AgentRun>(a => a.AgentName == "PaymentsHandoff" && a.InputTokens == 0 && !a.IsFallback),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_RejectsAnEmptyRequest_BeforeAnyModelCall()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ProcessTicketAsync(_ticket.Id, "   "));

        _intentExtractor.Verify(i => i.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _conversationRepository.Verify(r => r.SaveAsync(It.IsAny<CopilotConversation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SummarizeTicketAsync_SendsTheWholeTicket_Escaped()
    {
        var ticket = _ticket with { Description = "Locked out after <b>five</b> attempts & one reset." };
        _supportContext.Setup(c => c.GetTicketAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _supportContext
            .Setup(c => c.GetTicketMessagesAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TicketMessage { Content = "Still locked.", IsFromCustomer = true },
                new TicketMessage { Content = "Please try the reset link again." }
            ]);
        string? prompt = null;
        _chatClient
            .Setup(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<SupportChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback((string sent, SupportChatOptions? _, CancellationToken _) => prompt = sent)
            .ReturnsAsync(new SupportChatResponse { Content = "The customer is locked out." });

        await _service.SummarizeTicketAsync(ticket.Id);

        Assert.Contains("<status>Open</status>", prompt);
        Assert.Contains($"<description>{SecurityElement.Escape(ticket.Description)}</description>", prompt);
        Assert.DoesNotContain("<b>", prompt);
        Assert.Contains("Customer: Still locked.", prompt);
        Assert.Contains("Support: Please try the reset link again.", prompt);
    }

    [Fact]
    public async Task SummarizeTicketAsync_RecordsModelAndTokens_FromTheResponse()
    {
        _supportContext
            .Setup(c => c.GetTicketMessagesAsync(_ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TicketMessage { Content = "I am locked out.", IsFromCustomer = true }]);
        _chatClient
            .Setup(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<SupportChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportChatResponse { Content = "The customer is locked out.", ModelId = "test-model", InputTokens = 300, OutputTokens = 80 });

        var summary = await _service.SummarizeTicketAsync(_ticket.Id);

        Assert.Equal("The customer is locked out.", summary.Summary);
        Assert.Equal(300, summary.InputTokens);
        _agentRunRepository.Verify(r => r.LogAsync(
            It.Is<AgentRun>(a => a.AgentName == "SummarizationAgent" && a.TicketId == _ticket.Id && a.ModelId == "test-model" && a.OutputTokens == 80),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
