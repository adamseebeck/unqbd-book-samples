using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this activity's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    private const string Question = "Our job gets HTTP 429 responses from the orders endpoint. How should we handle the rate limit?";

    public static async Task<int> RunAsync(IServiceProvider services)
    {
        Console.WriteLine("Contoso Support Copilot: Chapter 9 activity check (answer focus judge)");

        var judges = services.GetKeyedServices<IEvaluator>(RagEvaluationService.JudgeKey);
        Console.WriteLine($"Judges: {string.Join(", ", judges.Select(j => j.GetType().Name))}");

        // A scripted judge stands in for the model, so the evaluator's two prompts run offline.
        var judge = new ChatConfiguration(new OfflineChatClient([new ScriptedFocusJudge()]));
        var evaluator = new AnswerFocusEvaluator();
        var generic = await ScoreAsync(evaluator, judge, "Please contact support for assistance.");
        var specific = await ScoreAsync(evaluator, judge, "Wait for the seconds in the Retry-After header, then retry the orders endpoint with exponential backoff.");
        Console.WriteLine($"Generic answer: {Format(generic)}; specific answer: {Format(specific)}");

        var passed = judges.Any(j => j is AnswerFocusEvaluator)
            && generic.Interpretation!.Failed
            && !specific.Interpretation!.Failed;
        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }

    private static async Task<NumericMetric> ScoreAsync(AnswerFocusEvaluator evaluator, ChatConfiguration judge, string answer)
    {
        var result = await evaluator.EvaluateAsync(
            [new ChatMessage(ChatRole.User, Question)],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)),
            judge);
        return result.Get<NumericMetric>(AnswerFocusEvaluator.AnswerFocusMetricName);
    }

    private static string Format(NumericMetric metric) =>
        $"{metric.Value?.ToString("0.00", CultureInfo.InvariantCulture)} ({(metric.Interpretation!.Failed ? "failed" : metric.Interpretation.Rating.ToString())})";

    // Answers the need prompt with the question itself, and scores an answer high when it uses the question's key words.
    private sealed class ScriptedFocusJudge : IOfflineResponder
    {
        public string? TryRespond(OfflineChatRequest request)
        {
            if (request.Instructions.StartsWith("State", StringComparison.Ordinal))
            {
                return new JsonObject { ["need"] = Question }.ToJsonString();
            }

            var answer = XElement.Parse(request.UserText[request.UserText.IndexOf("<answer>", StringComparison.Ordinal)..]).Value;
            var score = TextOverlap.ContentWords(answer).Overlaps(TextOverlap.ContentWords(Question)) ? 0.9 : 0.2;
            return new JsonObject { ["score"] = score, ["reason"] = "Scripted judgement from shared question words." }.ToJsonString();
        }
    }
}
