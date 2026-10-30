using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Validation;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class EscalationDraftTests
{
    [Theory]
    [InlineData("Medium", "1. Reset the password.", "recommendedPriority must be one of Low, Normal, High, Critical, not 'Medium'")]
    [InlineData("High", "Reset the password.", "reproductionSteps must be numbered steps or 'Not reproducible from the ticket'")]
    public void Validator_RejectsInvalidValues(string priority, string steps, string expectedError)
    {
        var draft = new EscalationDraft("Users are locked out.", "The team cannot work.", steps, priority);

        var result = new EscalationDraftValidator().Validate(draft);

        Assert.False(result.IsSuccess);
        Assert.Contains(expectedError, result.Errors);
    }

    [Fact]
    public void Render_DoesNotReplacePlaceholdersInsideValues()
    {
        var template = new PromptTemplate
        {
            Name = "Test",
            TemplateText = "A: {first} B: {second}",
            Parameters = ["first", "second"]
        };

        var text = template.Render(new Dictionary<string, object> { ["first"] = "{second}", ["second"] = "two" });

        Assert.Equal("A: {second} B: two", text);
    }

    [Fact]
    public void Render_Throws_WhenAParameterIsMissing()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            SupportPrompts.EscalationDraftExtraction.Render(new Dictionary<string, object> { ["subject"] = "Locked out" }));

        Assert.Contains("history", exception.Message);
    }

    [Fact]
    public async Task GenerateDraftAsync_SendsTheConversationHistory()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new SupportTicket
        {
            Id = ticketId,
            Subject = "Cannot sign in",
            Description = "The account is locked.",
            ProductArea = "Authentication",
            Customer = new CustomerAccount { Id = Guid.NewGuid(), Name = "Fabrikam", Tier = "Enterprise" },
            Messages =
            [
                new TicketMessage { Content = "Did you try the new password?", IsFromCustomer = false, SentAt = new DateTime(2026, 9, 14, 9, 20, 0, DateTimeKind.Utc) },
                new TicketMessage { Content = "The account is locked.", IsFromCustomer = true, SentAt = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc) }
            ]
        };

        var supportContext = new Mock<ISupportContext>();
        supportContext.Setup(c => c.GetTicketAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var orchestrator = new Mock<IKernelOrchestrator>();
        orchestrator
            .Setup(o => o.ExecutePluginFunctionAsync("SupportTicketPlugin", "calculate_priority_score", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("122.5");

        IReadOnlyDictionary<string, object>? sent = null;
        var executor = new Mock<IValidatedPromptExecutor>();
        executor
            .Setup(e => e.ExecuteAsync(SupportPrompts.EscalationDraftExtraction, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IOutputValidator<EscalationDraft>>(), It.IsAny<CancellationToken>()))
            .Callback<PromptTemplate, IReadOnlyDictionary<string, object>, IOutputValidator<EscalationDraft>, CancellationToken>((_, variables, _, _) => sent = variables)
            .ReturnsAsync(new EscalationDraft("Summary", "Impact", "1. Step", "High"));

        var service = new EscalationDraftService(supportContext.Object, orchestrator.Object, executor.Object, new EscalationDraftValidator());
        await service.GenerateDraftAsync(ticketId);

        Assert.NotNull(sent);
        Assert.Equal("122.5", sent["priorityScore"]);
        Assert.Equal($"Customer: The account is locked.{Environment.NewLine}Support: Did you try the new password?", sent["history"]);
    }
}
