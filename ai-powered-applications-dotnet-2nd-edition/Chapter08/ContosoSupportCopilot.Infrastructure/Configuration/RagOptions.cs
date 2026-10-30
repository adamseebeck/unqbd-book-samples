using System.ComponentModel.DataAnnotations;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

public class RagOptions
{
    public const string SectionName = "Rag";

    // The product areas each support engineer may read. In production, read them from
    // the group or role claims of the signed-in user instead of configuration.
    public Dictionary<string, string[]> ProductAreasByEngineer { get; set; } = new();

    [Range(100, 16_000, ErrorMessage = "Rag:MaxOutputTokens must be between 100 and 16000")]
    public int MaxOutputTokens { get; set; } = 1500;
}
