using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

public enum AIProvider
{
    Offline,
    Ollama,
    Foundry,
    OpenAI
}

public class AIProviderOptions : IValidatableObject
{
    public const string SectionName = "AI";

    public AIProvider Provider { get; set; } = AIProvider.Offline;

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string? ChatDeployment { get; set; }

    public string? EmbeddingDeployment { get; set; }

    public bool UseEntraId { get; set; }

    [Range(1, 300, ErrorMessage = "AI:TimeoutSeconds must be between 1 and 300")]
    public int TimeoutSeconds { get; set; } = 120;

    // Chapter 5: ask OpenAI and Foundry models for strict structured output. Set it to false
    // for a model or OpenAI-compatible server that rejects the "strict" setting.
    public bool StrictStructuredOutput { get; set; } = true;

    // Chapter 5: the reasoning effort for structured output, such as Low for simple classifications.
    // Leave it empty for a model without reasoning, which can reject the setting.
    public ReasoningEffort? StructuredOutputReasoningEffort { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Provider == AIProvider.Offline)
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(Endpoint) && !Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("AI:Endpoint must be an absolute URL", [nameof(Endpoint)]);
        }

        if (string.IsNullOrWhiteSpace(ChatDeployment))
        {
            yield return new ValidationResult($"AI:ChatDeployment is required for the {Provider} provider", [nameof(ChatDeployment)]);
        }

        if (string.IsNullOrWhiteSpace(EmbeddingDeployment))
        {
            yield return new ValidationResult($"AI:EmbeddingDeployment is required for the {Provider} provider", [nameof(EmbeddingDeployment)]);
        }

        if (Provider == AIProvider.Foundry)
        {
            if (Endpoint is null || !Endpoint.TrimEnd('/').EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult("AI:Endpoint must be the Foundry v1 endpoint, such as https://<resource>.openai.azure.com/openai/v1/", [nameof(Endpoint)]);
            }

            if (!UseEntraId && string.IsNullOrWhiteSpace(ApiKey))
            {
                yield return new ValidationResult("Set AI:ApiKey in user secrets, or set AI:UseEntraId to true", [nameof(ApiKey)]);
            }
        }

        if (Provider == AIProvider.OpenAI && string.IsNullOrWhiteSpace(ApiKey))
        {
            yield return new ValidationResult("AI:ApiKey is required for the OpenAI provider", [nameof(ApiKey)]);
        }
    }
}
