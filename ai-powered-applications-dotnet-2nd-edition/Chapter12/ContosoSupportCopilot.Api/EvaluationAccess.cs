namespace ContosoSupportCopilot.Api;

// The product areas of the identity the evaluation runs as (Evaluation:RequesterId): every area under test.
// Program.cs adds them only for a run with --check, which evaluates the dataset and never starts the web host,
// so no web request can claim an identity that reads everything.
public static class EvaluationAccess
{
    private static readonly string[] ProductAreas = ["Authentication", "Payments", "API", "Reporting"];

    // The keys name the configured identity, so a renamed Evaluation:RequesterId keeps its access. An engineer
    // who already has areas keeps only those, so a check run as that engineer measures what they may read.
    public static Dictionary<string, string?> Settings(IConfiguration configuration)
    {
        var requesterId = configuration["Evaluation:RequesterId"] ?? "evaluation-runner";
        if (configuration.GetSection($"Rag:ProductAreasByEngineer:{requesterId}").Exists())
        {
            return new();
        }

        return ProductAreas
            .Select((area, index) => (Key: $"Rag:ProductAreasByEngineer:{requesterId}:{index}", Area: area))
            .ToDictionary(entry => entry.Key, entry => (string?)entry.Area);
    }
}
