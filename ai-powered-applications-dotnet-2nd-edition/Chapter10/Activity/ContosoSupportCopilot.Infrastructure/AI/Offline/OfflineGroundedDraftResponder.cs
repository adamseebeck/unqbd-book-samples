using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>
/// Answers drafting prompts that carry a knowledge answer (Chapter 10) offline: the reply restates the
/// answer's sentences without their citation markers, so a run without a model shows which facts the
/// draft received. It does not write like a model; it only makes the data flow visible.
/// </summary>
public sealed partial class OfflineGroundedDraftResponder : IOfflineResponder
{
    private const string Start = "<knowledge_answer>";
    private const string End = "</knowledge_answer>";

    public string? TryRespond(OfflineChatRequest request)
    {
        var start = request.UserText.IndexOf(Start, StringComparison.Ordinal);
        var end = request.UserText.IndexOf(End, StringComparison.Ordinal);
        if (start < 0 || end < start)
        {
            return null;
        }

        var answer = XElement.Parse(request.UserText[start..(end + End.Length)]).Value;
        var facts = CitationMarker().Replace(answer.Replace("[Offline sample answer]", string.Empty), string.Empty).Trim();
        return $"[Offline sample reply] Thank you for contacting Contoso Support. {facts} " +
               "We will update this ticket as soon as we have more information.";
    }

    [GeneratedRegex(@"\s*[[]Article: [^]]*[]]")]
    private static partial Regex CitationMarker();
}
