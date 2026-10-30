using System.Text.RegularExpressions;

namespace ContosoSupportCopilot.Infrastructure.Security;

// A first filter for known injection phrases. Attackers rephrase, so pair it with a service
// built for the task, such as Prompt Shields in Azure AI Content Safety.
public sealed partial class PromptInjectionDetector
{
    public bool DetectInjection(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            return JailbreakPatterns().IsMatch(content);
        }
        catch (RegexMatchTimeoutException)
        {
            // Text that keeps the patterns busy this long is suspicious too: fail closed.
            return true;
        }
    }

    // Whitespace in the pattern is ignored. Character classes such as [(] match a literal character.
    [GeneratedRegex("""
        ignore \s+ (all \s+)? (previous|above|prior) \s+ instructions
        | system \s* prompt
        | you \s+ are \s+ now \s+ (an? \s+)? (unrestricted|uncensored)
        | DAN \s* [(] do \s+ anything \s+ now [)]
        | [[] \s* insert \s+ .*? \s* here \s* []]
        | new \s+ instructions
        | override \s+ (previous|all) \s+ (instructions|commands)
        | disregard \s+ (all \s+)? (previous|above)
        | forget \s+ (everything|all \s+ previous)
        | </knowledge_base> \s* <system>
        | < \s* system \s* >
        | [{][{] .*? [}][}]
        """, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, matchTimeoutMilliseconds: 250)]
    private static partial Regex JailbreakPatterns();
}
