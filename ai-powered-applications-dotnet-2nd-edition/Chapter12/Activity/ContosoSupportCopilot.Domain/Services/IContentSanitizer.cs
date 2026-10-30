namespace ContosoSupportCopilot.Domain.Services;

public interface IContentSanitizer
{
    string Sanitize(string content);
    bool ContainsMaliciousPatterns(string content);
}
