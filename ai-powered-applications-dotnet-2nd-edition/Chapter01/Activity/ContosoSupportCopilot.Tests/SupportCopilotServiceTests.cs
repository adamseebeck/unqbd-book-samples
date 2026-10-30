using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class SupportCopilotServiceTests
{
    private static readonly SupportTicket Ticket = new()
    {
        Id = Guid.NewGuid(),
        Subject = "Report export times out",
        Description = "Exporting the monthly usage report to CSV times out after 30 seconds.",
        Priority = "High",
        ProductArea = "Reporting"
    };

    [Fact]
    public async Task DraftEscalationAsync_PassesTicketDataToOrchestrator()
    {
        var supportContext = new Mock<ISupportContext>();
        supportContext.Setup(c => c.GetTicketAsync(Ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Ticket);

        Dictionary<string, object>? variables = null;
        var orchestrator = new Mock<IKernelOrchestrator>();
        orchestrator
            .Setup(o => o.ExecutePromptAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object>, CancellationToken>((_, values, _) => variables = values)
            .ReturnsAsync("The CSV export query exceeds the request timeout.");

        var service = CreateService(supportContext, orchestrator);

        var draft = await service.DraftEscalationAsync(Ticket.Id, CancellationToken.None);

        Assert.NotNull(variables);
        Assert.Equal(Ticket.Subject, variables["subject"]);
        Assert.Equal(Ticket.Description, variables["description"]);
        Assert.Equal("High", variables["priority"]);
        Assert.Equal("Reporting", variables["productArea"]);
        Assert.Equal("The CSV export query exceeds the request timeout.", draft.EngineeringSummary);
        orchestrator.Verify(
            o => o.ExecutePromptAsync(It.Is<string>(p => p.Contains("{{$subject}}")), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DraftEscalationAsync_SavesTheDraft()
    {
        var supportContext = new Mock<ISupportContext>();
        supportContext.Setup(c => c.GetTicketAsync(Ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Ticket);
        var orchestrator = new Mock<IKernelOrchestrator>();
        orchestrator
            .Setup(o => o.ExecutePromptAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Analysis");

        var service = CreateService(supportContext, orchestrator);

        await service.DraftEscalationAsync(Ticket.Id, CancellationToken.None);

        supportContext.Verify(
            c => c.SaveEscalationDraftAsync(It.Is<EscalationDraft>(d => d.TicketId == Ticket.Id && d.EngineeringSummary == "Analysis"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DraftEscalationAsync_ThrowsForUnknownTicket()
    {
        var supportContext = new Mock<ISupportContext>();
        var orchestrator = new Mock<IKernelOrchestrator>();
        var service = CreateService(supportContext, orchestrator);

        await Assert.ThrowsAsync<ArgumentException>(() => service.DraftEscalationAsync(Guid.NewGuid(), CancellationToken.None));
        orchestrator.VerifyNoOtherCalls();
    }

    private static SupportCopilotService CreateService(Mock<ISupportContext> supportContext, Mock<IKernelOrchestrator> orchestrator) =>
        new(supportContext.Object, Mock.Of<ISupportChatClient>(), orchestrator.Object, Mock.Of<IAgentCoordinator>());
}
