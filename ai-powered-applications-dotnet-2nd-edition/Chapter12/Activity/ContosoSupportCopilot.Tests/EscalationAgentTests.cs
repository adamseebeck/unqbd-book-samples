using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Validation;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class EscalationAgentTests
{
    [Fact]
    public async Task ExecuteAsync_UnauthorizedAccess_HandsOffToAPerson()
    {
        var result = await RunWithFailureAsync(new UnauthorizedAccessException("The escalation queue denied access."));

        Assert.True(result.RequiresHandoff);
        Assert.Equal(AgentFailureMode.Authorization, result.Artifacts[AgentArtifactKeys.FailureMode]);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_AsksForARetryInsteadOfAHandoff()
    {
        // The chat client reports a time limit as TimeoutException (Chapter 4).
        var result = await RunWithFailureAsync(new TimeoutException());

        Assert.False(result.RequiresHandoff);
        Assert.Equal(AgentFailureMode.Transient, result.Artifacts[AgentArtifactKeys.FailureMode]);
    }

    [Fact]
    public async Task ExecuteAsync_Success_PublishesTheDraftAndThePriorityScore()
    {
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped<EscalationDraftService>());
        using var scope = host.Services.CreateScope();
        var context = await CreateContextAsync(scope.ServiceProvider);

        var result = await scope.ServiceProvider.GetRequiredService<IAgentCoordinator>().ExecuteAgentAsync(AgentNames.Escalation, context);

        Assert.False(result.RequiresHandoff);
        Assert.IsType<EscalationDraft>(result.Artifacts[AgentArtifactKeys.EscalationDraft]);
        Assert.IsType<double>(result.Artifacts[AgentArtifactKeys.PriorityScore]);
        Assert.IsType<DateTime>(result.Artifacts[AgentArtifactKeys.GeneratedAt]);
        var run = Assert.Single(await scope.ServiceProvider.GetRequiredService<IAgentRunRepository>().GetByConversationAsync(context.ConversationId));
        Assert.Equal(AgentNames.Escalation, run.AgentName);
        Assert.False(run.IsFallback);
    }

    private static async Task<AgentResult> RunWithFailureAsync(Exception failure)
    {
        var executor = new Mock<IValidatedPromptExecutor>();
        executor.Setup(e => e.ExecuteAsync(It.IsAny<PromptTemplate>(), It.IsAny<IReadOnlyDictionary<string, object>>(),
                It.IsAny<IOutputValidator<EscalationDraft>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        using var host = await AgentTestHost.StartAsync(services =>
        {
            services.AddScoped<EscalationDraftService>();
            services.AddSingleton(executor.Object);
        });
        using var scope = host.Services.CreateScope();
        var context = await CreateContextAsync(scope.ServiceProvider);

        return await scope.ServiceProvider.GetRequiredService<IAgentCoordinator>().ExecuteAgentAsync(AgentNames.Escalation, context);
    }

    private static async Task<AgentContext> CreateContextAsync(IServiceProvider provider)
    {
        var conversation = new CopilotConversation { Id = Guid.NewGuid(), TicketId = SampleData.PasswordTicketId, CreatedAt = DateTime.UtcNow };
        await provider.GetRequiredService<IConversationRepository>().SaveAsync(conversation);
        var ticket = await provider.GetRequiredService<ISupportContext>().GetTicketAsync(SampleData.PasswordTicketId);
        return new AgentContext { ConversationId = conversation.Id, CurrentTicket = ticket, Intent = "escalation" };
    }
}
