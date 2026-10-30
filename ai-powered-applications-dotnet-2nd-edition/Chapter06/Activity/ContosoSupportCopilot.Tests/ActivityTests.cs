using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class ActivityTests
{
    [Fact]
    public async Task ProcessTicketAsync_StillDraftsAndSaves_WhenConfidenceIsLow()
    {
        var ticket = new SupportTicket { Id = Guid.NewGuid(), Subject = "Something seems off" };
        var supportContext = new Mock<ISupportContext>();
        supportContext.Setup(c => c.GetTicketAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var intentExtractor = new Mock<IIntentExtractor>();
        intentExtractor
            .Setup(i => i.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportIntent { Intent = "GeneralInquiry", Urgency = "Normal", ProductArea = "General", ConfidenceScore = 0.3 });
        var draftingService = new Mock<IDraftingService>();
        draftingService
            .Setup(d => d.DraftReplyAsync(ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftReply { Text = "Could you describe what you see?", ModelId = "test-model" });
        var conversationRepository = new Mock<IConversationRepository>();
        var correlationContext = new Mock<ICorrelationContext>();
        correlationContext.Setup(c => c.CorrelationId).Returns("test-correlation-id");

        var service = new SupportWorkflowService(
            intentExtractor.Object,
            draftingService.Object,
            Mock.Of<ISupportChatClient>(),
            supportContext.Object,
            correlationContext.Object,
            conversationRepository.Object,
            Mock.Of<IAgentRunRepository>());

        var result = await service.ProcessTicketAsync(ticket.Id, "Something seems off with my account.");

        Assert.Equal("Could you describe what you see?", result.Response);
        draftingService.Verify(d => d.DraftReplyAsync(ticket, It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        conversationRepository.Verify(r => r.SaveAsync(
            It.Is<CopilotConversation>(c => c.Turns.Count == 1 && c.Turns[0].CopilotResponse == "Could you describe what you see?"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FallbackIntentExtractor_LetsTheFailureThrough_WhenNothingIsCached()
    {
        var inner = new Mock<IIntentExtractor>();
        inner
            .Setup(i => i.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to extract intent: the model's answer failed validation."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateFallbackExtractor(inner.Object).ExtractAsync("The export fails."));
    }

    [Fact]
    public async Task FallbackIntentExtractor_ReturnsTheCachedIntent_WhenALaterCallFails()
    {
        var known = new SupportIntent { Intent = "BugReport", Urgency = "High", ProductArea = "Reporting", ConfidenceScore = 0.8 };
        var inner = new Mock<IIntentExtractor>();
        inner
            .SetupSequence(i => i.ExtractAsync("The export fails.", It.IsAny<CancellationToken>()))
            .ReturnsAsync(known)
            .ThrowsAsync(new TimeoutException("The AI service did not respond in time."));
        var extractor = CreateFallbackExtractor(inner.Object);

        await extractor.ExtractAsync("The export fails.");
        var intent = await extractor.ExtractAsync("The export fails.");

        Assert.Equal(known, intent);
    }

    [Fact]
    public async Task GetConversationHistoryAsync_ReturnsTheNewestTurnsFirst_OnePageAtATime()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<CopilotDbContext>().UseSqlite(connection).Options;
        var ticketId = Guid.NewGuid();

        await using (var context = new CopilotDbContext(options))
        {
            context.Database.EnsureCreated();
            await new SqlConversationRepository(context).SaveAsync(new CopilotConversation
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                CurrentTurn = 3,
                Turns =
                [
                    new ConversationTurn { TurnNumber = 1, UserInput = "one" },
                    new ConversationTurn { TurnNumber = 2, UserInput = "two" },
                    new ConversationTurn { TurnNumber = 3, UserInput = "three" }
                ]
            });
        }

        await using (var context = new CopilotDbContext(options))
        {
            var repository = new SqlConversationRepository(context);
            var firstPage = await repository.GetConversationHistoryAsync(ticketId, pageSize: 2, offset: 0);
            var secondPage = await repository.GetConversationHistoryAsync(ticketId, pageSize: 2, offset: 2);

            Assert.Equal(["three", "two"], firstPage.Select(t => t.UserInput));
            Assert.Equal(["one"], secondPage.Select(t => t.UserInput));
        }
    }

    private static FallbackIntentExtractor CreateFallbackExtractor(IIntentExtractor inner) => new(
        inner,
        NullLogger<FallbackIntentExtractor>.Instance,
        new MemoryCache(new MemoryCacheOptions()),
        Options.Create(new FallbackOptions()));
}
