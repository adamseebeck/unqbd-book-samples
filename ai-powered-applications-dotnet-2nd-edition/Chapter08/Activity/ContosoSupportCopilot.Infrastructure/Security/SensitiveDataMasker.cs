using System.Text.RegularExpressions;
using ContosoSupportCopilot.Domain.Services;

namespace ContosoSupportCopilot.Infrastructure.Security;

public sealed partial class SensitiveDataMasker : ISensitiveDataMasker
{
    // Returned instead of the text when a pattern times out: nothing unmasked reaches the prompt.
    public const string RedactedContent = "[CONTENT_REDACTED]";

    public string MaskSensitiveContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        try
        {
            // IP addresses first, so their digits are never read as part of a phone number.
            var masked = IpAddress().Replace(content, "[IP_REDACTED]");
            masked = Credential().Replace(masked, "[CREDENTIAL_REDACTED]");
            masked = BearerToken().Replace(masked, "[CREDENTIAL_REDACTED]");
            masked = EmailAddress().Replace(masked, "[EMAIL_REDACTED]");
            masked = ObfuscatedEmailAddress().Replace(masked, "[EMAIL_REDACTED]");
            return PhoneNumber().Replace(masked, "[PHONE_REDACTED]");
        }
        catch (RegexMatchTimeoutException)
        {
            // Retrieved text is untrusted, and crafted text can keep a pattern busy: fail closed.
            return RedactedContent;
        }
    }

    // Every pattern runs on untrusted text, so each has a match timeout, as in PromptInjectionDetector.
    [GeneratedRegex(@"\b\d{1,3}(?:[.]\d{1,3}){3}\b", RegexOptions.None, matchTimeoutMilliseconds: 250)]
    private static partial Regex IpAddress();

    // api_key=..., access_token: ..., DB_PASSWORD=... (a prefix such as "access_" is part of the name)
    [GeneratedRegex(@"(?<![A-Za-z0-9])(?:[A-Za-z0-9]+[_-])*(?:api[_-]?key|token|secret|password)\s*[:=]\s*\S{8,}", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 250)]
    private static partial Regex Credential();

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9._~+/-]{16,}=*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 250)]
    private static partial Regex BearerToken();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+[.][A-Za-z]{2,}\b", RegexOptions.None, matchTimeoutMilliseconds: 250)]
    private static partial Regex EmailAddress();

    // "ana at contoso dot com" and "ana [at] contoso [dot] com"
    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+\s*(?:[([]\s*at\s*[])]|\s+at\s+)\s*[A-Za-z0-9-]+\s*(?:[([]\s*dot\s*[])]|\s+dot\s+)\s*[A-Za-z]{2,}\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 250)]
    private static partial Regex ObfuscatedEmailAddress();

    // +1 555 010 0199, (555) 010-0199, 555.010.0199
    [GeneratedRegex(@"(?<!\w)(?:[+]\d{1,3}[ .-]?)?(?:[(]\d{3}[)]|\d{3})[ .-]?\d{3}[ .-]?\d{4}(?!\w)", RegexOptions.None, matchTimeoutMilliseconds: 250)]
    private static partial Regex PhoneNumber();
}
