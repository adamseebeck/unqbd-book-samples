using System.Text.RegularExpressions;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>A keyword-based classification of a support request, used for offline samples.</summary>
public sealed record OfflineClassification(string Intent, string Urgency, string ProductArea, double Confidence);

/// <summary>
/// Classifies support text with fixed keyword lists. It is deliberately simple and
/// deterministic: offline answers stay plausible for the sample tickets without a model.
/// </summary>
public static class OfflineSupportClassifier
{
    private static readonly KeywordRule[] ProductAreas =
    [
        new("Authentication", "password", "sign in", "sign-in", "log in", "login", "locked", "mfa", "two-factor", "sso"),
        new("Payments", "invoice", "payment", "charged", "charge", "refund", "subscription", "credit card"),
        new("API", "api", "endpoint", "webhook", "429", "rate limit", "sdk", "integration"),
        new("Reporting", "report", "dashboard", "export", "csv")
    ];

    private static readonly KeywordRule[] Intents =
    [
        new("PasswordReset", "password", "reset", "locked"),
        new("PaymentIssue", "charged", "refund", "invoice", "duplicate"),
        new("BugReport", "error", "bug", "crash", "fails", "failing", "broken", "exception"),
        new("FeatureRequest", "feature", "would like", "could you add", "suggestion"),
        new("HowToQuestion", "how do", "how can", "how should", "what is", "where can")
    ];

    private static readonly KeywordRule[] Urgencies =
    [
        new("Critical", "outage", "all users", "everyone", "data loss", "security breach", "production is down"),
        new("High", "urgent", "asap", "blocked", "cannot", "can't", "unable", "immediately"),
        new("Low", "when you have time", "minor", "cosmetic", "no rush")
    ];

    public static OfflineClassification Classify(string text)
    {
        var (area, areaHits) = BestMatch(ProductAreas, text, "General");
        var (intent, intentHits) = BestMatch(Intents, text, "GeneralInquiry");
        var (urgency, _) = BestMatch(Urgencies, text, "Normal", firstMatchWins: true);

        var hits = areaHits + intentHits;
        var confidence = hits == 0 ? 0.35 : Math.Min(0.95, 0.55 + (0.1 * hits));

        return new OfflineClassification(intent, urgency, area, Math.Round(confidence, 2));
    }

    private static (string Value, int Hits) BestMatch(KeywordRule[] rules, string text, string fallback, bool firstMatchWins = false)
    {
        var best = (Value: fallback, Hits: 0);
        foreach (var rule in rules)
        {
            var hits = rule.CountMatches(text);
            if (hits > best.Hits)
            {
                best = (rule.Value, hits);
                if (firstMatchWins)
                {
                    break;
                }
            }
        }

        return best;
    }

    private sealed class KeywordRule(string value, params string[] keywords)
    {
        private readonly Regex[] _patterns = keywords
            .Select(k => new Regex($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(k)}(?![\p{{L}}\p{{N}}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .ToArray();

        public string Value { get; } = value;

        public int CountMatches(string text) => _patterns.Count(p => p.IsMatch(text));
    }
}
