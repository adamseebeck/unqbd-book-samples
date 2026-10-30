using System.Text.RegularExpressions;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Security;

public sealed partial class ContentSanitizer : IContentSanitizer
{
    private const string Redacted = "[REDACTED]";

    private readonly PromptInjectionDetector _detector;

    public ContentSanitizer(PromptInjectionDetector detector)
    {
        _detector = detector;
    }

    public bool ContainsMaliciousPatterns(string content) => _detector.DetectInjection(content);

    // The second layer: an override phrase that slipped past detection loses its imperative.
    // RagPromptBuilder escapes markup, so the sanitizer leaves characters such as < and & alone.
    public string Sanitize(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        try
        {
            return OverridePhrase().Replace(content, Redacted);
        }
        catch (RegexMatchTimeoutException)
        {
            // Text that keeps the pattern busy this long does not reach the prompt.
            return Redacted;
        }
    }

    // Untrusted text, so the same match timeout as every other pattern in this chapter.
    [GeneratedRegex(@"\b(?:ignore|disregard|forget)\s+(?:all\s+)?(?:previous|above|prior)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 250)]
    private static partial Regex OverridePhrase();
}
