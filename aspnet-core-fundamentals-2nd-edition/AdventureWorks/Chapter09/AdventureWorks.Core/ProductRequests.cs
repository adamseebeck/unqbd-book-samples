using System.ComponentModel.DataAnnotations;

namespace AdventureWorks.Core;

public sealed class CreateProductRequest : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[A-Z]{3}-[0-9]{4}$")]
    public string Sku { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (!ProductCategories.All.Contains(Category))
            yield return new ValidationResult(
                "Unknown category.", [nameof(Category)]);
    }
}

public sealed class UpdateProductRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }
}
