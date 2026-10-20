using System.ComponentModel.DataAnnotations;
namespace Chapter07Sample.Models;

public sealed class AvailableSkuAttribute : AsyncValidationAttribute
{
    public AvailableSkuAttribute() => ErrorMessage = "{0} is unavailable in {1}.";

    public string CatalogName { get; set; } = "training catalog";

    public override string FormatMessage(string format, string name)
        => string.Format(System.Globalization.CultureInfo.CurrentCulture,
            format, name, CatalogName);
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        if (value is not string sku || string.IsNullOrWhiteSpace(sku))
            return ValidationResult.Success;
        var catalog = context.GetRequiredService<CatalogAvailability>();
        if (await catalog.IsAvailableAsync(sku, cancellationToken))
            return ValidationResult.Success;
        return new ValidationResult(FormatErrorMessage(context.DisplayName),
            context.MemberName is null ? Array.Empty<string>() : new[] { context.MemberName });
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
        => throw new InvalidOperationException("Use the asynchronous validation path.");
}
