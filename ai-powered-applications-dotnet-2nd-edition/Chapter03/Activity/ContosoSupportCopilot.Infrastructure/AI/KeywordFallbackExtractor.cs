using System.Text.RegularExpressions;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class KeywordFallbackExtractor : IFallbackIntentExtractor
{
    // Whole words only, with an optional plural "s", so "api" matches "APIs" but not "rapid".
    private static readonly (Regex Keyword, string ProductArea)[] Rules =
    [
        (Word("password"), "Authentication"),
        (Word("sign in"), "Authentication"),
        (Word("invoice"), "Payments"),
        (Word("refund"), "Payments"),
        (Word("api"), "API"),
        (Word("report"), "Reporting")
    ];

    public Task<SupportIntent> ExtractFallbackAsync(string ticketDescription, CancellationToken cancellationToken)
    {
        var productArea = Rules
            .Where(rule => rule.Keyword.IsMatch(ticketDescription))
            .Select(rule => rule.ProductArea)
            .FirstOrDefault() ?? "General";

        return Task.FromResult(new SupportIntent
        {
            Intent = "ManualReview",
            Urgency = "Normal",
            ProductArea = productArea,
            ConfidenceScore = 0.0
        });
    }

    private static Regex Word(string keyword) => new(
        $@"\b{Regex.Escape(keyword)}s?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
}
