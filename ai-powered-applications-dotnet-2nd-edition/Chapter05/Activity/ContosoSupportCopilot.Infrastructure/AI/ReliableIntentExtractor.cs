using System.Security;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Validation;
using Microsoft.Extensions.Logging;

namespace ContosoSupportCopilot.Infrastructure.AI;

public sealed class ReliableIntentExtractor : IIntentExtractor
{
    private readonly IValidatedPromptExecutor _executor;
    private readonly IOutputValidator<SupportIntent> _validator;
    private readonly ILogger<ReliableIntentExtractor> _logger;

    public ReliableIntentExtractor(
        IValidatedPromptExecutor executor,
        IOutputValidator<SupportIntent> validator,
        ILogger<ReliableIntentExtractor> logger)
    {
        _executor = executor;
        _validator = validator;
        _logger = logger;
    }

    public async Task<SupportIntent> ExtractAsync(
        string ticketDescription,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketDescription);

        // The template puts the text between XML tags, so it is escaped first.
        var variables = new Dictionary<string, object>
        {
            ["ticketDescription"] = SecurityElement.Escape(ticketDescription)
        };

        try
        {
            return await _executor.ExecuteAsync(SupportPrompts.IntentExtraction, variables, _validator, cancellationToken);
        }
        catch (SchemaValidationException ex)
        {
            _logger.LogError(ex, "Intent extraction failed validation after retries: {Errors}", string.Join("; ", ex.ValidationErrors));
            throw new InvalidOperationException("Failed to extract intent: the model's answer failed validation.", ex);
        }
    }
}
