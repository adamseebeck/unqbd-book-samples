using System.Xml.Linq;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Security;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// The agents run on the offline provider, so the Agent Framework calls the tools for real.
public sealed class AgentTests
{
    [Fact]
    public async Task TriageAgent_ReadsItsArtifactsFromTheToolResults()
    {
        using var host = await AgentTestHost.StartAsync();
        using var scope = host.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<SupportCopilotService>().TriageTicketAsync(SampleData.PasswordTicketId);

        Assert.False(result.RequiresHandoff);
        Assert.Equal("PasswordReset", result.Artifacts[AgentArtifactKeys.Intent]);
        Assert.Equal("Authentication", result.Artifacts[AgentArtifactKeys.ProductArea]);
        Assert.IsType<double>(result.Artifacts[AgentArtifactKeys.PriorityScore]);
        Assert.Contains("classify_ticket returned", result.Response);
    }

    [Fact]
    public async Task TriageAgent_HandsOffToEscalation_WhenTheClassificationIsUncertain()
    {
        var extractor = new Mock<IIntentExtractor>();
        extractor.Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportIntent { Intent = "GeneralInquiry", Urgency = "Normal", ProductArea = "General", ConfidenceScore = 0.4 });
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped(_ => extractor.Object));
        using var scope = host.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<SupportCopilotService>().TriageTicketAsync(SampleData.PasswordTicketId);

        Assert.True(result.RequiresHandoff);
        Assert.Equal(AgentNames.Escalation, result.NextAgent);
    }

    [Fact]
    public async Task KnowledgeAgent_PassesOnTheCitedSources_WithExcerptsOnly()
    {
        using var host = await AgentTestHost.StartAsync();
        using var scope = host.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<SupportCopilotService>()
            .FindKnowledgeAsync(SampleData.PasswordTicketId, "engineer-ana");

        var citations = Assert.IsAssignableFrom<IReadOnlyList<CitationReference>>(result.Artifacts[AgentArtifactKeys.Citations]);
        Assert.False(result.RequiresHandoff);
        Assert.NotEmpty(citations);
        Assert.All(citations, c => { Assert.Empty(c.Content); Assert.NotEmpty(c.Excerpt); });
    }

    [Fact]
    public async Task KnowledgeAgent_KeepsOneSessionPerEngineer()
    {
        using var host = await AgentTestHost.StartAsync();
        using var scope = host.Services.CreateScope();
        var copilot = scope.ServiceProvider.GetRequiredService<SupportCopilotService>();

        var ana = await copilot.FindKnowledgeAsync(SampleData.PasswordTicketId, "engineer-ana");
        await copilot.FindKnowledgeAsync(SampleData.PasswordTicketId, "engineer-ben");

        var conversation = await scope.ServiceProvider.GetRequiredService<IConversationRepository>().GetByTicketAsync(SampleData.PasswordTicketId);
        var sessions = scope.ServiceProvider.GetRequiredService<IAgentSessionRepository>();
        var anaSession = await sessions.GetAsync(conversation!.Id, AgentNames.Knowledge, "engineer-ana");
        var benSession = await sessions.GetAsync(conversation.Id, AgentNames.Knowledge, "engineer-ben");

        // engineer-ben's session never holds the Authentication answer that engineer-ana received.
        Assert.NotNull(anaSession);
        Assert.NotNull(benSession);
        Assert.Contains("Restore access after a password reset", anaSession.SessionJson);
        Assert.DoesNotContain("Restore access after a password reset", benSession.SessionJson);
        Assert.False(ana.RequiresHandoff);
    }

    [Fact]
    public async Task KnowledgeAgent_WithoutAnEngineer_FailsAsAnAuthorizationProblem()
    {
        using var host = await AgentTestHost.StartAsync();
        using var scope = host.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<SupportCopilotService>()
            .FindKnowledgeAsync(SampleData.PasswordTicketId, engineerId: "");

        Assert.True(result.RequiresHandoff);
        Assert.Equal(AgentFailureMode.Authorization, result.Artifacts[AgentArtifactKeys.FailureMode]);
    }

    [Fact]
    public async Task DraftingAgent_FallbackReply_GoesToAPersonAndIsRecorded()
    {
        var drafting = new Mock<IDraftingService>();
        drafting.Setup(d => d.DraftReplyAsync(It.IsAny<SupportTicket>(), It.IsAny<DraftingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftReply { Text = "A support engineer will review your ticket shortly.", IsFallback = true });
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped(_ => drafting.Object));
        using var scope = host.Services.CreateScope();
        var context = new AgentContext { ConversationId = Guid.NewGuid(), CurrentTicket = new SupportTicket { Id = SampleData.PasswordTicketId } };

        var result = await scope.ServiceProvider.GetRequiredService<IAgentCoordinator>().ExecuteAgentAsync(AgentNames.Drafting, context);

        Assert.True(result.RequiresHandoff);
        var run = Assert.Single(await scope.ServiceProvider.GetRequiredService<IAgentRunRepository>().GetByConversationAsync(context.ConversationId));
        Assert.True(run.IsFallback);
        Assert.Equal(AgentNames.Drafting, run.AgentName);
    }

    [Fact]
    public async Task DraftGroundedReply_HandsTheCitedAnswerToTheDraft()
    {
        using var host = await AgentTestHost.StartAsync();
        using var scope = host.Services.CreateScope();
        var copilot = scope.ServiceProvider.GetRequiredService<SupportCopilotService>();

        var knowledge = await copilot.FindKnowledgeAsync(SampleData.PasswordTicketId, "engineer-ana");
        var draft = await copilot.DraftGroundedReplyAsync(SampleData.PasswordTicketId, "engineer-ana");

        // Offline, the draft restates the knowledge answer, so its first fact appears without the citation marker.
        var firstFact = knowledge.Response.Replace("[Offline sample answer] ", string.Empty).Split(" [Article:")[0];
        Assert.False(draft.RequiresHandoff);
        Assert.Contains(firstFact, draft.Response);
        Assert.DoesNotContain("[Article:", draft.Response);
    }

    [Fact]
    public async Task DraftGroundedReply_SendsAnUngroundedAnswerToEscalation_WithoutDrafting()
    {
        var drafting = new Mock<IDraftingService>();
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped(_ => drafting.Object));
        using var scope = host.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<SupportCopilotService>()
            .DraftGroundedReplyAsync(SampleData.PasswordTicketId, "engineer-ben");

        Assert.True(result.RequiresHandoff);
        Assert.Equal(AgentNames.Escalation, result.NextAgent);
        drafting.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DraftGroundedReply_ReturnsATransientKnowledgeFailure_WithoutDrafting()
    {
        var drafting = new Mock<IDraftingService>();
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped(_ => drafting.Object));
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IAgentCoordinator>()
            .RegisterAgentAsync(AgentNames.Knowledge, new UnavailableKnowledgeAgent());

        var result = await scope.ServiceProvider.GetRequiredService<SupportCopilotService>()
            .DraftGroundedReplyAsync(SampleData.PasswordTicketId, "engineer-ana");

        // A transient failure asks for a retry, not a person, and it is never drafted into a customer reply.
        Assert.False(result.RequiresHandoff);
        Assert.Equal(AgentFailureMode.Transient, result.Artifacts[AgentArtifactKeys.FailureMode]);
        drafting.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SummarizeTicketAsync_RemovesCharactersXmlCannotHold_BeforeTheMaskerReadsThem()
    {
        // Pasted terminal colors, a form feed, NUL and lone surrogates would make ToString throw. Removed only
        // after masking, the NUL inside the address would hide it from the masker and then let it through.
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            Subject = "Locked out\u001B[0m\f",
            Description = "Reset fails for ana\u0000@contoso.com\uD800\tsee the log\nThanks \U0001F600",
            Status = "Open\u0007"
        };
        var support = new Mock<ISupportContext>();
        support.Setup(s => s.GetTicketAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        support.Setup(s => s.GetTicketMessagesAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TicketMessage { Content = "Still locked\u000B out\uDC00", IsFromCustomer = true }]);
        string? prompt = null;
        var chat = new Mock<ISupportChatClient>();
        chat.Setup(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<SupportChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback((string text, SupportChatOptions? _, CancellationToken _) => prompt = text)
            .ReturnsAsync(new SupportChatResponse { Content = "The customer is locked out." });
        var copilot = new SupportCopilotService(
            support.Object, chat.Object, Mock.Of<IAgentCoordinator>(), Mock.Of<IConversationRepository>(), new SensitiveDataMasker());

        await copilot.SummarizeTicketAsync(ticket.Id);

        var sent = XElement.Parse(prompt!);
        Assert.Equal("Locked out[0m", sent.Element("subject")?.Value);
        Assert.Equal("Reset fails for [EMAIL_REDACTED]\tsee the log\nThanks \U0001F600", sent.Element("description")?.Value);
        Assert.Equal("Open", sent.Element("status")?.Value);
        Assert.Equal("Still locked out", sent.Element("messages")?.Element("message")?.Value);
    }

    [Fact]
    public async Task DraftingService_RemovesCharactersXmlCannotHold_FromTheKnowledge()
    {
        // An excerpt cut at 200 characters can end in half an emoji, and an answer or a title can carry pasted
        // control characters. ToString would throw on them, and the fallback reply would replace the grounded draft.
        var source = new CitationReference
        {
            ArticleId = Guid.NewGuid(),
            ArticleTitle = "Restore access\u000B after a reset",
            Excerpt = "Clear the lockout\u001B[0m from the user page \uD83D..."
        };
        var knowledge = new DraftingKnowledge("Clear the lockout.\f [Article: Restore access after a reset]", [source]);
        List<ChatMessage>? messages = null;
        var chat = new Mock<IChatClient>();
        chat.Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<ChatMessage> sent, ChatOptions? _, CancellationToken _) => messages = sent.ToList())
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Please clear the lockout from the user page.")));
        var service = new DraftingService(chat.Object, NullLogger<DraftingService>.Instance);

        var draft = await service.DraftReplyAsync(
            new SupportTicket { Id = Guid.NewGuid(), Subject = "Locked out" }, new DraftingOptions { Knowledge = knowledge });

        var prompt = messages![^1].Text;
        Assert.Equal("Clear the lockout. [Article: Restore access after a reset]", PromptElement(prompt, "knowledge_answer").Value);
        var excerpt = Assert.Single(PromptElement(prompt, "sources").Elements("source"));
        Assert.Equal("Restore access after a reset", (string?)excerpt.Attribute("title"));
        Assert.Equal("Clear the lockout[0m from the user page ...", excerpt.Value);
        Assert.Equal("Please clear the lockout from the user page.", draft.Text);
    }

    [Fact]
    public async Task KnowledgeAgent_AddsItsTurnAfterATurnThatAnotherRequestSavedFirst()
    {
        using var host = await AgentTestHost.StartAsync(services => services.AddScoped<IConversationRepository>(sp =>
            new OtherRequestSavesFirst(new SqlConversationRepository(sp.GetRequiredService<CopilotDbContext>()))));
        using var scope = host.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<SupportCopilotService>()
            .FindKnowledgeAsync(SampleData.PasswordTicketId, "engineer-ana");

        var conversation = await scope.ServiceProvider.GetRequiredService<IConversationRepository>().GetByTicketAsync(SampleData.PasswordTicketId);
        Assert.False(result.RequiresHandoff);
        Assert.Equal(["The other request", result.Response], conversation!.Turns.OrderBy(t => t.TurnNumber).Select(t => t.CopilotResponse));
        Assert.Equal(2, conversation.CurrentTurn);
    }

    [Fact]
    public async Task AgentRunner_TreatsAFilteredAnswerAsContentSafety_AndKeepsNoSession()
    {
        using var host = await AgentTestHost.StartAsync();
        using var scope = host.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<AgentRunner>();
        var context = new AgentContext { ConversationId = Guid.NewGuid(), RequesterId = "engineer-ana" };
        using var agent = new OpenTelemetryAgent(new ChatClientAgent(new FilteredAnswerChatClient(),
            new ChatClientAgentOptions { Name = "FilteredAgent" }), SupportAgentFactory.TelemetrySourceName);

        var outcome = await runner.RunAsync(agent, context, "Write the reply.", [], CancellationToken.None);

        Assert.Equal(AgentFailureMode.ContentSafety, outcome.FailureMode);
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IAgentSessionRepository>()
            .GetAsync(context.ConversationId, "FilteredAgent", "engineer-ana"));
    }

    [Fact]
    public async Task SupportAgentFactory_DisposingAnAgent_LeavesTheSharedChatClientWorking()
    {
        using var host = await AgentTestHost.StartAsync();
        var factory = host.Services.GetRequiredService<SupportAgentFactory>();

        using (var first = factory.Create("FirstAgent", "Answer briefly.", []))
        {
            await first.RunAsync("Hello");
        }

        using var second = factory.Create("SecondAgent", "Answer briefly.", []);
        var response = await second.RunAsync("Hello again");

        Assert.NotEmpty(response.Text);
    }

    [Fact]
    public async Task Coordinator_PrefersRegisteredAgents_AndRejectsUnknownNames()
    {
        using var host = await AgentTestHost.StartAsync();
        using var scope = host.Services.CreateScope();
        var coordinator = scope.ServiceProvider.GetRequiredService<IAgentCoordinator>();
        var stub = new Mock<IAgent>();
        stub.Setup(a => a.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AgentResult { Response = "stub" });

        await coordinator.RegisterAgentAsync(AgentNames.Triage, stub.Object);
        var result = await coordinator.ExecuteAgentAsync(AgentNames.Triage, new AgentContext());
        var handedOff = await coordinator.HandoffAsync(AgentNames.Knowledge, AgentNames.Triage, new AgentContext());

        Assert.Equal("stub", result.Response);
        Assert.Equal("stub", handedOff.Response);
        stub.Verify(a => a.ExecuteAsync(It.Is<AgentContext>(c => c.Metadata.ContainsKey("handoff_from")), It.IsAny<CancellationToken>()), Times.Once);
        await Assert.ThrowsAsync<ArgumentException>(() => coordinator.ExecuteAgentAsync("UnknownAgent", new AgentContext()));
    }

    [Fact]
    public async Task SupportAgentFactory_SharesOneAgentPerName_AndEachRunUsesItsOwnTools()
    {
        using var host = await AgentTestHost.StartAsync();
        var factory = host.Services.GetRequiredService<SupportAgentFactory>();
        var agent = factory.GetAgent("LookupAgent", "Call lookup_value, then report what it returns.");

        // Two runs at once on the shared agent, each with a tool that returns its own value.
        var runs = await Task.WhenAll(
            agent.RunAsync("Which value?", null, WithTool("first")),
            agent.RunAsync("Which value?", null, WithTool("second")));

        Assert.Same(agent, factory.GetAgent("LookupAgent", "Call lookup_value, then report what it returns."));
        Assert.Contains("first", runs[0].Text);
        Assert.DoesNotContain("second", runs[0].Text);
        Assert.Contains("second", runs[1].Text);
    }

    [Fact]
    public async Task AgentRunner_AddsItsTurn_ToASessionThatAnotherRunSavedFirst()
    {
        using var host = await AgentTestHost.StartAsync();
        using var first = host.Services.CreateScope();
        using var second = host.Services.CreateScope();
        var agent = host.Services.GetRequiredService<SupportAgentFactory>().GetAgent("RaceAgent", "Answer briefly.");
        var context = new AgentContext { ConversationId = Guid.NewGuid(), RequesterId = "engineer-ana" };
        var sessions = first.ServiceProvider.GetRequiredService<IAgentSessionRepository>();

        // The second run starts and saves while the first one is still running: just before the first save.
        var race = new SaveRace(sessions, () => second.ServiceProvider.GetRequiredService<AgentRunner>()
            .RunAsync(agent, context, "The second question", [], CancellationToken.None));
        var runner = ActivatorUtilities.CreateInstance<AgentRunner>(first.ServiceProvider, race);

        var outcome = await runner.RunAsync(agent, context, "The first question", [], CancellationToken.None);

        var stored = await sessions.GetAsync(context.ConversationId, "RaceAgent", "engineer-ana");
        Assert.Null(outcome.FailureMode);
        Assert.Equal(2, stored!.Version);
        Assert.Contains("The first question", stored.SessionJson);
        Assert.Contains("The second question", stored.SessionJson);
    }

    // One element of a prompt, such as <sources>, parsed back from the text the model received.
    private static XElement PromptElement(string prompt, string name)
    {
        var start = prompt.IndexOf($"<{name}>", StringComparison.Ordinal);
        var end = prompt.IndexOf($"</{name}>", StringComparison.Ordinal) + name.Length + 3;
        return XElement.Parse(prompt[start..end]);
    }

    private static ChatClientAgentRunOptions WithTool(string value) =>
        new(new ChatOptions { Tools = [AIFunctionFactory.Create(() => value, "lookup_value", "Returns the value for this run.")] });

    // Runs another request's save just before the first save of this run, as a concurrent request would.
    private sealed class SaveRace(IAgentSessionRepository inner, Func<Task> otherRun) : IAgentSessionRepository
    {
        private bool _raced;

        public Task<AgentSessionRecord?> GetAsync(Guid conversationId, string agentName, string requesterId, CancellationToken cancellationToken = default) =>
            inner.GetAsync(conversationId, agentName, requesterId, cancellationToken);

        public async Task<int> SaveAsync(AgentSessionRecord session, CancellationToken cancellationToken = default)
        {
            if (!_raced)
            {
                _raced = true;
                await otherRun();
            }

            return await inner.SaveAsync(session, cancellationToken);
        }
    }

    // Saves another request's turn just before the agent's first save of a turn, then reports the conflict
    // that the CurrentTurn concurrency token raises in that situation (Chapter 6).
    private sealed class OtherRequestSavesFirst(IConversationRepository inner) : IConversationRepository
    {
        private bool _saved;

        public Task<CopilotConversation?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default) =>
            inner.GetAsync(conversationId, cancellationToken);

        public Task<CopilotConversation?> GetByTicketAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
            inner.GetByTicketAsync(ticketId, cancellationToken);

        public async Task SaveAsync(CopilotConversation conversation, CancellationToken cancellationToken = default)
        {
            if (!_saved && conversation.Turns.Count > 0)
            {
                _saved = true;
                var current = (await inner.GetAsync(conversation.Id, cancellationToken))!;
                var other = new ConversationTurn { TurnNumber = current.CurrentTurn + 1, UserInput = "Other", CopilotResponse = "The other request", Timestamp = DateTime.UtcNow };
                await inner.SaveAsync(current with { CurrentTurn = other.TurnNumber, Turns = [.. current.Turns, other] }, cancellationToken);
                throw new ConversationConflictException(conversation.TicketId, new InvalidOperationException("Another request saved first."));
            }

            await inner.SaveAsync(conversation, cancellationToken);
        }
    }

    // A knowledge agent whose model calls timed out: the transient failure that every agent returns then.
    private sealed class UnavailableKnowledgeAgent : IAgent
    {
        public string Name => AgentNames.Knowledge;

        public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(AgentResults.Failure(Name, AgentFailureMode.Transient));
    }

    // A provider whose content filter stops the answer: a normal response with the ContentFilter finish reason.
    private sealed class FilteredAnswerChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty)) { FinishReason = ChatFinishReason.ContentFilter });

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }
}
