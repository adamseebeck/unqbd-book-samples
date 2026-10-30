using System.Globalization;
using ContosoSupportCopilot.Infrastructure.Evaluation;
using ContosoSupportCopilot.Infrastructure.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class AnswerFocusEvaluatorTests
{
    private const string Question = "Our job gets HTTP 429 responses from the orders endpoint. How should we handle the rate limit?";

    [Theory]
    [InlineData("Please contact support for assistance.", 0.2, true)]
    [InlineData("Wait for the seconds in the Retry-After header, then retry with exponential backoff.", 0.9, false)]
    public async Task EvaluateAsync_ScoresAGenericAnswerLow_AndASpecificAnswerHigh(string answer, double judgeScore, bool failed)
    {
        var judge = JudgeAnswering(judgeScore);

        var result = await new AnswerFocusEvaluator().EvaluateAsync(
            [new ChatMessage(ChatRole.User, Question)],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)),
            new ChatConfiguration(judge.Object));

        var metric = result.Get<NumericMetric>(AnswerFocusEvaluator.AnswerFocusMetricName);
        Assert.Equal(judgeScore, metric.Value);
        Assert.Equal(failed, metric.Interpretation!.Failed);

        // Two calls: the information need first, then the judgement.
        judge.Verify(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EvaluateAsync_ReportsAnError_WithoutAJudgeModel()
    {
        var result = await new AnswerFocusEvaluator().EvaluateAsync(
            [new ChatMessage(ChatRole.User, Question)],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "Retry later.")));

        var metric = result.Get<NumericMetric>(AnswerFocusEvaluator.AnswerFocusMetricName);
        Assert.Null(metric.Value);
        Assert.True(metric.ContainsDiagnostics(d => d.Severity == EvaluationDiagnosticSeverity.Error));
    }

    [Fact]
    public void AddEvaluationServices_RegistersTheEvaluator_AsAJudge()
    {
        var services = new ServiceCollection();
        services.AddEvaluationServices(new ConfigurationBuilder().Build());

        var judges = services.BuildServiceProvider().GetKeyedServices<IEvaluator>(RagEvaluationService.JudgeKey);

        Assert.Contains(judges, judge => judge is AnswerFocusEvaluator);
    }

    // A stand-in for the judge model: the first request asks for the need, the second for the score.
    private static Mock<IChatClient> JudgeAnswering(double score)
    {
        var judge = new Mock<IChatClient>();
        judge
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<ChatMessage> _, ChatOptions? options, CancellationToken _) =>
                new ChatResponse(new ChatMessage(ChatRole.Assistant, options!.Instructions!.StartsWith("State")
                    ? """{"need":"How to handle HTTP 429 responses from the orders endpoint"}"""
                    : $$"""{"score":{{score.ToString(CultureInfo.InvariantCulture)}},"reason":"Judged by the test."}""")));
        return judge;
    }
}
