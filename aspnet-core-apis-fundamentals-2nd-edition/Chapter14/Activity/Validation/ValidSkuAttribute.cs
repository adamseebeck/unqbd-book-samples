using System.ComponentModel.DataAnnotations;
using WarehouseApi.RoutingConstraints;

namespace WarehouseApi.Validation;

public class ValidSkuAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        // [Required] reports a missing value; this attribute checks the format
        if (value is not string sku || string.IsNullOrEmpty(sku))
        {
            return ValidationResult.Success;
        }

        // The pattern lives in one place, the Chapter 5 route constraint, so URLs and bodies cannot drift apart
        if (!SkuRouteConstraint.IsValidSku(sku))
        {
            return new ValidationResult(
                "SKU must be 2 to 4 uppercase letters, a hyphen, and 3 to 8 digits (for example, SKU-001).");
        }

        return ValidationResult.Success;
    }
}
