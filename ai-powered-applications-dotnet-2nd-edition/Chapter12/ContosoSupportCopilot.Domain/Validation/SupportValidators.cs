using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Validation;

public static class SupportPriorities
{
    // The urgency and priority values the rest of the application understands.
    public static IReadOnlyList<string> All { get; } = ["Low", "Normal", "High", "Critical"];
}

public sealed class SupportIntentValidator : IOutputValidator<SupportIntent>
{
    public ValidationResult<SupportIntent> Validate(SupportIntent value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Intent))
        {
            errors.Add("intent must not be empty");
        }

        if (!SupportPriorities.All.Contains(value.Urgency))
        {
            errors.Add($"urgency must be one of {string.Join(", ", SupportPriorities.All)}, not '{value.Urgency}'");
        }

        if (string.IsNullOrWhiteSpace(value.ProductArea))
        {
            errors.Add("productArea must not be empty");
        }

        if (value.ConfidenceScore is < 0.0 or > 1.0)
        {
            errors.Add($"confidenceScore must be between 0.0 and 1.0, not {value.ConfidenceScore}");
        }

        return errors.Count == 0
            ? ValidationResult<SupportIntent>.Success(value)
            : ValidationResult<SupportIntent>.Failure(errors);
    }
}

public sealed class EscalationDraftValidator : IOutputValidator<EscalationDraft>
{
    public ValidationResult<EscalationDraft> Validate(EscalationDraft value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Summary))
        {
            errors.Add("summary must not be empty");
        }

        if (string.IsNullOrWhiteSpace(value.Impact))
        {
            errors.Add("impact must not be empty");
        }

        if (string.IsNullOrWhiteSpace(value.ReproductionSteps))
        {
            errors.Add("reproductionSteps must not be empty");
        }

        if (!SupportPriorities.All.Contains(value.RecommendedPriority))
        {
            errors.Add($"recommendedPriority must be one of {string.Join(", ", SupportPriorities.All)}, not '{value.RecommendedPriority}'");
        }

        return errors.Count == 0
            ? ValidationResult<EscalationDraft>.Success(value)
            : ValidationResult<EscalationDraft>.Failure(errors);
    }
}
