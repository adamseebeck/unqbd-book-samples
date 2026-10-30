using System.Xml.Linq;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>
/// Answers grounded prompts offline: it quotes the first sentence of up to two articles from the
/// knowledge base in the prompt and cites each one, so citation checks run without a model.
/// </summary>
public sealed class OfflineGroundedAnswerResponder : IOfflineResponder
{
    private const string Start = "<knowledge_base>";
    private const string End = "</knowledge_base>";

    public string? TryRespond(OfflineChatRequest request)
    {
        var start = request.UserText.IndexOf(Start, StringComparison.Ordinal);
        var end = request.UserText.IndexOf(End, StringComparison.Ordinal);
        if (start < 0 || end < start)
        {
            return null;
        }

        var knowledgeBase = XElement.Parse(request.UserText[start..(end + End.Length)]);
        var sentences = knowledgeBase.Elements("article")
            .Take(2)
            .Select(article => $"{FirstSentence(article.Value)} [Article: {(string?)article.Attribute("title")} ({(string?)article.Attribute("id")})]")
            .ToList();

        return sentences.Count == 0
            ? "[Offline sample answer] The knowledge base does not contain the answer."
            : "[Offline sample answer] " + string.Join(" ", sentences);
    }

    private static string FirstSentence(string text)
    {
        var trimmed = text.Trim();
        var end = trimmed.IndexOfAny(['.', '!', '?']);
        return end < 0 ? trimmed : trimmed[..(end + 1)];
    }
}
