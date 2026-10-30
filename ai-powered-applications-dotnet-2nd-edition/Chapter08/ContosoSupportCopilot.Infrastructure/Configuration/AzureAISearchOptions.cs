using System.ComponentModel.DataAnnotations;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

public class AzureAISearchOptions : IValidatableObject
{
    public const string SectionName = "Search";

    [Required(ErrorMessage = "Search:Endpoint is required for Azure AI Search")]
    [Url(ErrorMessage = "Search:Endpoint must be a URL such as https://<service>.search.windows.net")]
    public string Endpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "Search:IndexName is required for Azure AI Search")]
    public string IndexName { get; set; } = "contoso-knowledge";

    public string? ApiKey { get; set; }

    public bool UseEntraId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!UseEntraId && string.IsNullOrWhiteSpace(ApiKey))
        {
            yield return new ValidationResult(
                "Set Search:ApiKey in user secrets, or set Search:UseEntraId to true",
                [nameof(ApiKey)]);
        }
    }
}
