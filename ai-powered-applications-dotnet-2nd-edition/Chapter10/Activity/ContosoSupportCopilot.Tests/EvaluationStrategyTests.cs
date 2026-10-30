using ContosoSupportCopilot.Domain.Evaluation;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class EvaluationStrategyTests
{
    [Fact]
    public async Task RetrievalOptimizer_LowersTheFloor_WhenRecallIsLow()
    {
        var recommendation = await new RetrievalOptimizer().OptimizeAsync(
            new RagEvaluationMetrics { Precision = 1, Recall = 0.5, CitationUsefulnessScore = 1 },
            new ChunkingOptions(),
            new VectorSearchRequest { TopK = 3, MinScore = 0.5 });

        Assert.Equal(new VectorSearchRequest { TopK = 6, MinScore = 0.45 }, recommendation.SuggestedSearchRequest);
        Assert.Null(recommendation.SuggestedChunkingOptions);
    }

    [Fact]
    public async Task RetrievalOptimizer_RaisesTheFloor_WhenPrecisionIsLow_ButNeverAbove09()
    {
        var recommendation = await new RetrievalOptimizer().OptimizeAsync(
            new RagEvaluationMetrics { Precision = 0.4, Recall = 0.9, CitationUsefulnessScore = 1 },
            new ChunkingOptions(),
            new VectorSearchRequest { TopK = 4, MinScore = 0.85 });

        Assert.Equal(new VectorSearchRequest { TopK = 3, MinScore = 0.9 }, recommendation.SuggestedSearchRequest);
    }

    [Fact]
    public async Task RetrievalOptimizer_NeverRaisesTopK_ForPrecision_AndSuggestsNothingAtTheLimits()
    {
        var optimizer = new RetrievalOptimizer();
        var lowPrecision = new RagEvaluationMetrics { Precision = 0.4, Recall = 0.9, CitationUsefulnessScore = 1 };

        var fewResults = await optimizer.OptimizeAsync(lowPrecision, new ChunkingOptions(), new VectorSearchRequest { TopK = 2, MinScore = 0.5 });
        var atTheLimits = await optimizer.OptimizeAsync(lowPrecision, new ChunkingOptions(), new VectorSearchRequest { TopK = 3, MinScore = 0.9 });

        Assert.Equal(new VectorSearchRequest { TopK = 2, MinScore = 0.6 }, fewResults.SuggestedSearchRequest);
        Assert.False(atTheLimits.ChangesRecommended);
    }

    [Fact]
    public async Task RetrievalOptimizer_SuggestsSmallerChunks_WhenCitationsAreNotUseful()
    {
        var recommendation = await new RetrievalOptimizer().OptimizeAsync(
            new RagEvaluationMetrics { Precision = 1, Recall = 1, CitationUsefulnessScore = 0.4 },
            new ChunkingOptions { MaxChunkSize = 512, OverlapSize = 50 },
            new VectorSearchRequest());

        Assert.Equal(new ChunkingOptions { MaxChunkSize = 409, OverlapSize = 60 }, recommendation.SuggestedChunkingOptions);
        Assert.Null(recommendation.SuggestedSearchRequest);
    }

    [Fact]
    public async Task PromptRevisionStrategy_AddsEachRuleOnce_AndKeepsTheVersion_WhenNothingChanges()
    {
        var weak = new RagEvaluationResult
        {
            Metrics = new RagEvaluationMetrics { SourceOverlapRatio = 0.5, CitationUsefulnessScore = 1 },
            LowOverlapSentences = ["Refunds arrive within one hour."]
        };
        var strategy = new PromptRevisionStrategy();

        var once = await strategy.ReviseAsync(SupportPrompts.GroundedAnswer, weak);
        var twice = await strategy.ReviseAsync(once, weak);

        Assert.Equal("1.1", once.Version);
        Assert.Contains(PromptRevisionStrategy.GroundingRule, once.Instructions);
        Assert.Contains(PromptRevisionStrategy.CitationRule, once.Instructions);
        Assert.Equal(SupportPrompts.GroundedAnswer.TemplateText, once.TemplateText);

        // Revising 1.1 for the same reasons adds nothing, so it stays 1.1.
        Assert.Same(once, twice);
    }

    [Fact]
    public async Task PromptRevisionStrategy_AddsTheGroundingRule_WhenOnlyTheJudgeFailsTheAnswer()
    {
        // Every deterministic metric passes, so the judge's score alone makes the answer need revision.
        var contradicted = new RagEvaluationResult { Metrics = Good with { GroundednessScore = 1 } };

        var revised = await new PromptRevisionStrategy().ReviseAsync(SupportPrompts.GroundedAnswer, contradicted);

        Assert.True(contradicted.RequiresRevision);
        Assert.Contains(PromptRevisionStrategy.GroundingRule, revised.Instructions);
        Assert.DoesNotContain(PromptRevisionStrategy.CitationRule, revised.Instructions);
    }

    [Fact]
    public async Task PromptRevisionStrategy_KeepsTheTemplate_WhenTheResultNeedsNoRevision()
    {
        var good = new RagEvaluationResult { Metrics = Good };

        var result = await new PromptRevisionStrategy().ReviseAsync(SupportPrompts.GroundedAnswer, good);

        Assert.False(good.RequiresRevision);
        Assert.Same(SupportPrompts.GroundedAnswer, result);
    }

    [Fact]
    public async Task PromptRevisionStrategy_AddsTheCitationRule_ForAnAnswerThatCitesAnArticleItWasNotGiven()
    {
        var invented = new RagEvaluationResult { Metrics = Good with { CitationsValid = false } };

        var revised = await new PromptRevisionStrategy().ReviseAsync(SupportPrompts.GroundedAnswer, invented);

        Assert.True(invented.RequiresRevision);
        Assert.Contains(PromptRevisionStrategy.CitationRule, revised.Instructions);
    }

    // The gate covers retrieval too: a miss answered faithfully from the wrong articles still needs revision.
    [Theory]
    [InlineData(0.5, 1.0, true)]
    [InlineData(1.0, 0.6, true)]
    [InlineData(0.0, 0.0, true)]
    [InlineData(0.6, 0.7, false)]
    public void RequiresRevision_FlagsRetrievalBelowTheFloors(double precision, double recall, bool expected)
    {
        var result = new RagEvaluationResult { Metrics = Good with { Precision = precision, Recall = recall } };

        Assert.Equal(expected, result.RequiresRevision);
    }

    private static RagEvaluationMetrics Good => new() { Precision = 1, Recall = 1, SourceOverlapRatio = 1, CitationUsefulnessScore = 1 };
}
