using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using Microsoft.Extensions.AI;
using Moq;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class AgentToolFunctionTests
{
    private static readonly AgentToolContext Context = new("corr-1", Guid.NewGuid(), null,
        new SupportTicket { Id = Guid.NewGuid(), Subject = "Cannot sign in", ProductArea = "Authentication" }, "engineer-ana");

    [Fact]
    public void JsonSchema_ExposesOnlyTheArgumentsTheModelSupplies()
    {
        var function = new AgentToolFunction(CreateTool().Object, Context);

        var properties = function.JsonSchema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("query", out _));
        Assert.False(properties.TryGetProperty("requesterId", out _));
        Assert.Equal("search_knowledge_base", function.Name);
    }

    [Fact]
    public async Task InvokeAsync_DeserializesTheArguments_AndRecordsTheCall()
    {
        var tool = CreateTool();
        var function = new AgentToolFunction(tool.Object, Context);

        var output = await function.InvokeAsync(new AIFunctionArguments { ["query"] = "locked account" });

        var call = Assert.Single(function.Calls);
        Assert.Equal(new KnowledgeSearchInput("locked account"), call.Input);
        Assert.Equal("model view", output);
        tool.Verify(t => t.ExecuteAsync(It.IsAny<object>(), Context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAnArgumentOutsideTheInputType_FailsWithoutRunningTheTool()
    {
        var tool = CreateTool();
        var function = new AgentToolFunction(tool.Object, Context);

        // The model cannot choose who it searches for: requesterId is not part of the input.
        await function.InvokeAsync(new AIFunctionArguments { ["query"] = "x", ["requesterId"] = "engineer-ben" });

        var call = Assert.Single(function.Calls);
        Assert.False(call.Result.IsSuccess);
        tool.Verify(t => t.ExecuteAsync(It.IsAny<object>(), It.IsAny<AgentToolContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public void Classify_MapsExceptionsToRecoveries(Exception exception, AgentFailureMode expected) =>
        Assert.Equal(expected, AgentFailureClassifier.Classify(exception));

    public static TheoryData<Exception, AgentFailureMode> Failures() => new()
    {
        { new TimeoutRejectedException(), AgentFailureMode.Transient },
        { new BrokenCircuitException(), AgentFailureMode.Transient },
        { new HttpRequestException(), AgentFailureMode.Transient },
        { new UnauthorizedAccessException(), AgentFailureMode.Authorization },
        { new AggregateException(new TimeoutException()), AgentFailureMode.Transient },
        { new InvalidOperationException("The circuit is open.", new BrokenCircuitException()), AgentFailureMode.Transient },
        { new InvalidOperationException(), AgentFailureMode.Unrecoverable }
    };

    [Fact]
    public async Task OfflineToolCalling_CallsTheToolsTheInstructionsName_InThatOrder()
    {
        var client = new OfflineToolCallingChatClient(new OfflineChatClient());
        var options = new ChatOptions
        {
            Instructions = "Call second_tool, then first_tool.",
            Tools = [Tool("first_tool"), Tool("second_tool"), Tool("unused_tool")]
        };

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "reset link expired")], options);

        var calls = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ToList();
        Assert.Equal(["second_tool", "first_tool"], calls.Select(c => c.Name));
        Assert.Equal("reset link expired", calls[0].Arguments!["value"]);
        Assert.Equal(ChatFinishReason.ToolCalls, response.FinishReason);
    }

    [Fact]
    public async Task OfflineToolCalling_AfterTheResults_AnswersWithText()
    {
        var client = new OfflineToolCallingChatClient(new OfflineChatClient());
        var options = new ChatOptions { Instructions = "Call first_tool.", Tools = [Tool("first_tool")] };
        List<ChatMessage> messages =
        [
            new(ChatRole.User, "question"),
            new(ChatRole.Assistant, [new FunctionCallContent("c1", "first_tool")]),
            new(ChatRole.Tool, [new FunctionResultContent("c1", "42")])
        ];

        var response = await client.GetResponseAsync(messages, options);

        Assert.Equal("[Offline sample response] first_tool returned 42.", response.Text);
    }

    private static AIFunction Tool(string name) =>
        AIFunctionFactory.Create((string value) => value, name: name, description: name);

    private static Mock<IAgentTool> CreateTool()
    {
        var tool = new Mock<IAgentTool>();
        tool.SetupGet(t => t.Name).Returns("search_knowledge_base");
        tool.SetupGet(t => t.Description).Returns("Searches the knowledge base.");
        tool.SetupGet(t => t.InputType).Returns(typeof(KnowledgeSearchInput));
        tool.SetupGet(t => t.OutputType).Returns(typeof(RagResult));
        tool.Setup(t => t.ExecuteAsync(It.IsAny<object>(), It.IsAny<AgentToolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentToolResult.Success(new RagResult(), "model view"));
        return tool;
    }
}
