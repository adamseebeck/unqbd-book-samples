using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WarehouseApi.Validation;

public partial class ValidWarehouseCodeAttribute : ValidationAttribute
{
    // Warehouse codes compare without regard to case, so wh-001 is as valid as WH-001
    [GeneratedRegex(@"^WH-[0-9]{3}\z", RegexOptions.IgnoreCase)]
    private static partial Regex WarehouseCodePattern();

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        // [Required] reports a missing value; this attribute checks the format
        if (value is not string code || string.IsNullOrEmpty(code))
        {
            return ValidationResult.Success;
        }

        return WarehouseCodePattern().IsMatch(code)
            ? ValidationResult.Success
            : new ValidationResult("Warehouse code must be WH- followed by exactly three digits (for example, WH-001).");
    }
}
