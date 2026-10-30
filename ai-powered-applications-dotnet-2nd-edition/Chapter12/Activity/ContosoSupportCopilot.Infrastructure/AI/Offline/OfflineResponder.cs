using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>Builds the deterministic sample answers that <see cref="OfflineChatClient"/> returns.</summary>
internal static partial class OfflineResponder
{
    public static string CreateResponse(OfflineChatRequest request, IReadOnlyList<IOfflineResponder> responders)
    {
        foreach (var responder in responders)
        {
            if (responder.TryRespond(request) is { } custom)
            {
                return custom;
            }
        }

        var classification = OfflineSupportClassifier.Classify(request.UserText);
        var format = request.JsonFormat;

        // A JSON schema gives the exact shape. Without one, use the JSON example or
        // field list that the instructions describe.
        if (format?.Schema is { } schema)
        {
            return OfflineJson.FromSchema(schema, classification);
        }

        if (format is not null || request.AllText.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            if (OfflineJson.FromInstructions(request.AllText, classification) is { } json)
            {
                return json;
            }

            if (format is not null)
            {
                return new JsonObject { ["response"] = CreateText(request, classification) }.ToJsonString();
            }
        }

        return CreateText(request, classification);
    }

    public static string CreateText(OfflineChatRequest request, OfflineClassification c)
    {
        var topic = c.ProductArea == "General" ? "your request" : $"your {c.ProductArea} request";
        var text = request.AllText;

        if (text.Contains("summar", StringComparison.OrdinalIgnoreCase))
        {
            return $"[Offline sample summary] Product area: {c.ProductArea}. Intent: {c.Intent}. Urgency: {c.Urgency}. " +
                   $"Recommended next step: {NextStep(c.ProductArea)}.";
        }

        if (text.Contains("draft", StringComparison.OrdinalIgnoreCase) || text.Contains("reply", StringComparison.OrdinalIgnoreCase))
        {
            return $"[Offline sample reply] Thank you for contacting Contoso Support about {topic}. " +
                   $"The next step is to {NextStep(c.ProductArea)}. We will update this ticket as soon as we have more information.";
        }

        return "[Offline sample response] This answer was generated locally without a model. " +
               $"Product area: {c.ProductArea}. Intent: {c.Intent}. Urgency: {c.Urgency}.";
    }

    public static string NextStep(string productArea) => productArea switch
    {
        "Authentication" => "send a password reset link and confirm that the account is not locked",
        "Payments" => "confirm the charge on the latest invoice and refund it if it is a duplicate",
        "API" => "compare the request rate with the plan limit and retry with exponential backoff after 429 responses",
        "Reporting" => "reproduce the report and attach the exported file to the ticket",
        _ => "collect the steps that lead to the problem and a screenshot of any error"
    };

    public static UsageDetails EstimateUsage(OfflineChatRequest request, string output)
    {
        // Offline usage is an estimate (about four characters per token), not a provider count.
        var input = request.Messages.Sum(m => EstimateTokens(m.Text)) + EstimateTokens(request.Options?.Instructions);
        var outputTokens = EstimateTokens(output);

        return new UsageDetails
        {
            InputTokenCount = input,
            OutputTokenCount = outputTokens,
            TotalTokenCount = input + outputTokens,
            CachedInputTokenCount = 0,
            ReasoningTokenCount = 0
        };
    }

    public static long EstimateTokens(string? text) =>
        string.IsNullOrEmpty(text) ? 0 : Math.Max(1, (text.Length + 3) / 4);

    public static IEnumerable<string> SplitForStreaming(string text) =>
        StreamingPiece().Matches(text).Select(m => m.Value);

    [GeneratedRegex(@"\S+\s*")]
    private static partial Regex StreamingPiece();
}
