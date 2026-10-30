using System.Text.RegularExpressions;

namespace ContosoSupportCopilot.Domain.Prompts;

public sealed partial record PromptTemplate
{
    public required string Name { get; init; }
    public string? Instructions { get; init; }
    public required string TemplateText { get; init; }
    public IReadOnlyList<string> Parameters { get; init; } = [];
    public string Description { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0";

    public string Render(IReadOnlyDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var missing = Parameters.Where(p => !variables.ContainsKey(p)).ToList();
        if (missing.Count > 0)
        {
            throw new ArgumentException($"Missing required parameters: {string.Join(", ", missing)}", nameof(variables));
        }

        // One pass over the template, so a value that contains {name} is never replaced again.
        return Placeholder().Replace(TemplateText, match =>
            Parameters.Contains(match.Groups[1].Value)
                ? variables[match.Groups[1].Value]?.ToString() ?? string.Empty
                : match.Value);
    }

    [GeneratedRegex("[{]([A-Za-z_][A-Za-z0-9_]*)[}]")]
    private static partial Regex Placeholder();
}
