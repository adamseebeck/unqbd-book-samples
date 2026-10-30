using ContosoSupportCopilot.Domain.Evaluation;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Evaluation;
using ContosoSupportCopilot.Infrastructure.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// The judge path, offline. A scripted judge stands in for the model, so these tests check the evaluator
// wiring, the judge client, and the gate; how well a real judge scores needs a model and labeled answers.
public sealed class JudgeTests : IDisposable
{
    private static readonly Guid SignInErrors = Guid.NewGuid();
    private const string Source = "Never send a password or a reset code in a ticket reply. Reset links expire after one hour.";

    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"contoso-judges-{Guid.NewGuid():N}");
    private readonly Mock<IRagOrchestrator> _orchestrator = new();

    [Theory]
    [InlineData("Send the password in the ticket reply. Reset links expire after 24 hours.", 1.0, true)]
    [InlineData("Never send a password in a ticket reply. Reset links expire after one hour.", 5.0, false)]
    public async Task EvaluateDatasetAsync_FailsTheContradiction_ThatSourceOverlapPasses(string answer, double groundedness, bool requiresRevision)
    {
        AnswerWith(answer, cited: true);

        var report = await CreateService(new ScriptedJudge()).EvaluateDatasetAsync(Dataset(), SupportPrompts.GroundedAnswer, Search(), "judged");

        var result = Assert.Single(report.Results);
        Assert.Equal(1.0, result.Metrics.SourceOverlapRatio);
        Assert.Equal(groundedness, result.Metrics.GroundednessScore);
        Assert.Equal(requiresRevision, result.RequiresRevision);
    }

    [Fact]
    public async Task EvaluateDatasetAsync_CountsAnInventedCitation_AsNeedingRevision()
    {
        // A faithful sentence from the real source, plus a citation of an article the model never received.
        AnswerWith("Never send a password in a ticket reply. Reset links expire after one hour.", cited: true, grounded: false);

        var report = await CreateService(new ScriptedJudge()).EvaluateDatasetAsync(Dataset(), SupportPrompts.GroundedAnswer, Search(), "judged");

        var result = Assert.Single(report.Results);
        Assert.Equal(5.0, result.Metrics.GroundednessScore);
        Assert.False(result.Metrics.CitationsValid);
        Assert.True(result.RequiresRevision);
        Assert.Equal(1, report.FailedTests);
        Assert.Contains(PromptRevisionStrategy.CitationRule, report.SuggestedPromptTemplate?.Instructions);
    }

    [Fact]
    public async Task EvaluateDatasetAsync_Throws_WhenAJudgeCallFails()
    {
        AnswerWith("Never send a password in a ticket reply.", cited: true);
        var judge = new ScriptedJudge { Failure = new InvalidOperationException("HTTP 400: Unsupported parameter: 'temperature'.") };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(judge).EvaluateDatasetAsync(Dataset(), SupportPrompts.GroundedAnswer, Search(), "judged"));

        Assert.Contains("Groundedness judge returned no score", error.Message);
        Assert.Contains("temperature", error.Message);
    }

    [Fact]
    public async Task EvaluateDatasetAsync_SkipsTheJudges_ForARefusalWithoutSources()
    {
        AnswerWith("I cannot find any relevant knowledge base articles for this query.", cited: false);
        var judge = new ScriptedJudge();

        var report = await CreateService(judge).EvaluateDatasetAsync(Dataset(), SupportPrompts.GroundedAnswer, Search(), "judged");

        Assert.Equal(0, judge.Calls);
        Assert.Null(Assert.Single(report.Results).Metrics.GroundednessScore);
    }

    [Fact]
    public async Task JudgeChatClient_RemovesSamplingOptions_AndRaisesTheOutputLimit()
    {
        var judge = new ScriptedJudge();

        await new GroundednessEvaluator().EvaluateAsync(
            [new ChatMessage(ChatRole.User, "Can I send the password in the ticket reply?")],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "Never send a password in a ticket reply.")),
            new ChatConfiguration(new JudgeChatClient(judge, 4000)),
            [new GroundednessEvaluatorContext(Source)]);

        Assert.NotNull(judge.ReceivedOptions);
        Assert.Null(judge.ReceivedOptions.Temperature);
        Assert.Null(judge.ReceivedOptions.TopP);
        Assert.Null(judge.ReceivedOptions.PresencePenalty);
        Assert.Null(judge.ReceivedOptions.FrequencyPenalty);
        Assert.Equal(4000, judge.ReceivedOptions.MaxOutputTokens);
    }

    [Fact]
    public async Task JudgeChatClient_Throws_WhenTheJudgmentStopsAtTheOutputLimit()
    {
        var client = new JudgeChatClient(new ScriptedJudge { FinishReason = ChatFinishReason.Length }, 4000);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "**Groundedness**")]));

        Assert.Contains("Evaluation:JudgeMaxOutputTokens", error.Message);
    }

    [Fact]
    public async Task AddEvaluationServices_RunsEachJudgeCallThroughOnePipeline_AndLetsTheFailureThrough()
    {
        // A judge model that cannot be reached: the pipeline makes its three attempts, and the error reaches
        // the caller. Without the pipeline there would be one attempt; with a second one, nine.
        var model = new UnreachableModel();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:Provider"] = "Offline",
            ["KernelResilience:BaseDelayMilliseconds"] = "1",
            ["KernelResilience:MaxDelayMilliseconds"] = "1"
        });
        builder.Services.AddSingleton<IOfflineResponder>(model);
        builder.Services.AddAIProvider(builder.Configuration);
        builder.Services.AddSemanticKernelServices(builder.Configuration);
        builder.Services.AddEvaluationServices(builder.Configuration);
        using var host = builder.Build();
        var judge = host.Services.GetRequiredKeyedService<IChatClient>(RagEvaluationService.JudgeKey);

        await Assert.ThrowsAsync<HttpRequestException>(() => judge.GetResponseAsync([new ChatMessage(ChatRole.User, "**Groundedness**")]));

        Assert.Equal(3, model.Calls);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    private void AnswerWith(string answer, bool cited, bool grounded = true)
    {
        var source = new CitationReference { ArticleId = SignInErrors, ArticleTitle = "Troubleshoot sign-in errors", Content = Source };
        var rag = cited
            ? new RagResult { Response = answer, Citations = [source], CitedArticleIds = [SignInErrors], IsGrounded = grounded, ModelId = "test-model" }
            : new RagResult { Response = answer };
        _orchestrator
            .Setup(o => o.GenerateGroundedResponseAsync(It.IsAny<RagRequest>(), It.IsAny<SupportTicket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rag);
    }

    // The judges run here although the provider is offline, because UseJudges switches them on.
    private RagEvaluationService CreateService(IChatClient judge) => new(
        _orchestrator.Object,
        [new RetrievalPrecisionEvaluator(), new CitationUsefulnessEvaluator(), new SourceOverlapEvaluator()],
        [new GroundednessEvaluator()],
        new JudgeChatClient(judge, 4000),
        new RetrievalOptimizer(),
        new PromptRevisionStrategy(),
        Options.Create(new EvaluationOptions { StoragePath = _storagePath, UseJudges = true, JudgeDeployment = "judge-model" }),
        Options.Create(new AIProviderOptions()),
        NullLogger<RagEvaluationService>.Instance);

    private static EvaluationDataset Dataset()
    {
        var dataset = new EvaluationDataset { Name = "judge-tests" };
        dataset.AddTestCase(
            "password-in-reply",
            "Can I send the customer the new password in the ticket reply?",
            "Authentication",
            "Never send a password in a ticket reply.",
            [SignInErrors]);
        return dataset;
    }

    private static VectorSearchRequest Search() => new() { TopK = 3, MinScore = 0.2 };

    // Answers the Groundedness prompt with the verdict that a judge model gives each answer, in the tagged
    // format the evaluator parses. It records the options it received and can fail or stop at the limit.
    private sealed class ScriptedJudge : IChatClient
    {
        public Exception? Failure { get; init; }
        public ChatFinishReason FinishReason { get; init; } = ChatFinishReason.Stop;
        public ChatOptions? ReceivedOptions { get; private set; }
        public int Calls { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            ReceivedOptions = options;
            if (Failure is not null)
            {
                throw Failure;
            }

            var prompt = string.Join("\n", messages.Select(m => m.Text));
            var verdict = prompt.Contains("Send the password in the ticket reply", StringComparison.Ordinal)
                ? "<S0>The context forbids it.</S0><S1>The response reverses the instruction.</S1><S2>1</S2>"
                : "<S0>The response restates the context.</S0><S1>Fully supported.</S1><S2>5</S2>";
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, verdict)) { FinishReason = FinishReason });
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    // Stands in for a judge deployment that refuses every connection.
    private sealed class UnreachableModel : IOfflineResponder
    {
        public int Calls { get; private set; }

        public string? TryRespond(OfflineChatRequest request)
        {
            Calls++;
            throw new HttpRequestException("Simulated connection failure");
        }
    }
}
