using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Validation;

public class ValidReasonCodeAttribute : ValidationAttribute
{
    private static readonly string[] ValidCodes =
    [
        "RECEIPT", "DAMAGE", "CORRECTION", "CYCLE_COUNT", "RETURN"
    ];

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        // [Required] reports a missing value; this attribute checks the vocabulary
        if (value is not string code || string.IsNullOrEmpty(code))
        {
            return ValidationResult.Success;
        }

        if (!ValidCodes.Contains(code))
        {
            return new ValidationResult(
                $"Reason code must be one of: {string.Join(", ", ValidCodes)}.");
        }

        return ValidationResult.Success;
    }
}
