using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Infrastructure.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class RagEvaluatorTests
{
    private static readonly Guid Refunds = Guid.NewGuid();
    private static readonly Guid Lockouts = Guid.NewGuid();

    private static readonly CitationReference RefundSource = new()
    {
        ArticleId = Refunds,
        ArticleTitle = "Refund a duplicate charge",
        Content = "Duplicate charges usually come from a retried payment. The refund takes three to five business days."
    };

    [Theory]
    [InlineData(true, "refunds", 1.0, 1.0)]
    [InlineData(true, "lockouts", 0.0, 0.0)]
    [InlineData(false, "refunds", 0.0, 0.0)]
    [InlineData(true, "none", 0.0, 1.0)]
    [InlineData(false, "none", 1.0, 1.0)]
    public async Task RetrievalPrecisionEvaluator_ComparesRetrievedAndRelevantArticles(
        bool retrieved, string relevant, double expectedPrecision, double expectedRecall)
    {
        var rag = new RagResult { Citations = retrieved ? [RefundSource] : [] };
        Guid[] relevantIds = relevant switch { "refunds" => [Refunds], "lockouts" => [Lockouts], _ => [] };

        var result = await new RetrievalPrecisionEvaluator().EvaluateAsync(
            [], new ChatResponse(), additionalContext: [new RagEvaluationContext(rag, relevantIds)]);

        Assert.Equal(expectedPrecision, result.Get<NumericMetric>(RetrievalPrecisionEvaluator.PrecisionMetricName).Value);
        Assert.Equal(expectedRecall, result.Get<NumericMetric>(RetrievalPrecisionEvaluator.RecallMetricName).Value);
    }

    [Fact]
    public async Task CitationUsefulnessEvaluator_ScoresACitedSourceThatTheAnswerUses()
    {
        var answer = $"The refund takes three to five business days [Article: Refund a duplicate charge ({Refunds})].";
        var rag = new RagResult { Response = answer, Citations = [RefundSource], CitedArticleIds = [Refunds], IsGrounded = true };

        var result = await Evaluate(new CitationUsefulnessEvaluator(), rag, [Refunds]);

        Assert.Equal(1.0, result.Get<NumericMetric>(CitationUsefulnessEvaluator.UsefulnessMetricName).Value);
        Assert.True(result.Get<BooleanMetric>(CitationUsefulnessEvaluator.ValidityMetricName).Value);
    }

    [Fact]
    public async Task CitationUsefulnessEvaluator_GivesZero_WhenTheCitedSourceIsNotUsed()
    {
        var answer = $"Restart the router and wait for the lights [Article: Refund a duplicate charge ({Refunds})].";
        var rag = new RagResult { Response = answer, Citations = [RefundSource], CitedArticleIds = [Refunds], IsGrounded = true };

        var result = await Evaluate(new CitationUsefulnessEvaluator(), rag, [Refunds]);

        Assert.Equal(0.0, result.Get<NumericMetric>(CitationUsefulnessEvaluator.UsefulnessMetricName).Value);
        Assert.True(result.Get<NumericMetric>(CitationUsefulnessEvaluator.UsefulnessMetricName).Interpretation!.Failed);
    }

    [Fact]
    public async Task SourceOverlapEvaluator_FlagsASentenceThatSharesNoWordsWithTheSources()
    {
        var answer = "Duplicate charges usually come from a retried payment. Refunds arrive within one hour.";
        var rag = new RagResult { Response = answer, Citations = [RefundSource], CitedArticleIds = [Refunds], IsGrounded = true };

        var result = await Evaluate(new SourceOverlapEvaluator(), rag, [Refunds]);

        Assert.Equal(0.5, result.Get<NumericMetric>(SourceOverlapEvaluator.SourceOverlapMetricName).Value);
        Assert.Equal(["Refunds arrive within one hour."], SourceOverlapEvaluator.FindLowOverlapSentences(answer, [RefundSource.Content]));
    }

    // The metric's blind spot, stated as a test: a reversed instruction and a wrong number built from the
    // source's own words pass. JudgeTests shows the groundedness judge failing the same answer.
    [Theory]
    [InlineData("Send the password in the ticket reply.")]
    [InlineData("Reset links expire after 24 hours.")]
    public async Task SourceOverlapEvaluator_PassesAContradiction_ThatReusesTheSourcesWords(string answer)
    {
        var source = new CitationReference
        {
            ArticleId = Lockouts,
            ArticleTitle = "Troubleshoot sign-in errors",
            Content = "Never send a password or a reset code in a ticket reply. Reset links expire after one hour."
        };
        var rag = new RagResult { Response = answer, Citations = [source], CitedArticleIds = [Lockouts], IsGrounded = true };

        var result = await Evaluate(new SourceOverlapEvaluator(), rag, [Lockouts]);

        Assert.Equal(1.0, result.Get<NumericMetric>(SourceOverlapEvaluator.SourceOverlapMetricName).Value);
    }

    [Fact]
    public async Task Evaluators_AcceptARefusal_WhenNothingIsRelevant()
    {
        var rag = new RagResult { Response = "I cannot find any relevant knowledge base articles for this query." };

        var citations = await Evaluate(new CitationUsefulnessEvaluator(), rag, []);
        var overlap = await Evaluate(new SourceOverlapEvaluator(), rag, []);

        Assert.Equal(1.0, citations.Get<NumericMetric>(CitationUsefulnessEvaluator.UsefulnessMetricName).Value);
        Assert.Equal(1.0, overlap.Get<NumericMetric>(SourceOverlapEvaluator.SourceOverlapMetricName).Value);
    }

    private static async Task<EvaluationResult> Evaluate(IEvaluator evaluator, RagResult rag, Guid[] relevantIds) =>
        await evaluator.EvaluateAsync(
            [new ChatMessage(ChatRole.User, "question")],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, rag.Response)),
            additionalContext: [new RagEvaluationContext(rag, relevantIds)]);
}
