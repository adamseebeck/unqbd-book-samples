namespace ContosoSupportCopilot.Domain.Evaluation;

public class EvaluationDataset
{
    // The name becomes part of every scenario name in the evaluation report.
    public string Name { get; init; } = string.Empty;
    public List<EvaluationTestCase> TestCases { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public void AddTestCase(
        string name,
        string query,
        string productArea,
        string expectedAnswer,
        IReadOnlyList<Guid> relevantArticleIds)
    {
        TestCases.Add(new EvaluationTestCase
        {
            Name = name,
            Query = query,
            ProductArea = productArea,
            ExpectedAnswer = expectedAnswer,
            RelevantArticleIds = relevantArticleIds
        });
    }
}

public record EvaluationTestCase
{
    // A short, stable name, so results from different runs line up in the report.
    public string Name { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public string ProductArea { get; init; } = string.Empty;
    public string ExpectedAnswer { get; init; } = string.Empty;

    // Empty for a question that the knowledge base should not answer.
    public IReadOnlyList<Guid> RelevantArticleIds { get; init; } = Array.Empty<Guid>();
}
