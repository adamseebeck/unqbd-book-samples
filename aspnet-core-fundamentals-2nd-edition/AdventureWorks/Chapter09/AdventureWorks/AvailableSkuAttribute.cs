using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AdventureWorks;

public sealed class AvailableSkuAttribute
    : AsyncValidationAttribute
{
    public AvailableSkuAttribute() =>
        ErrorMessage = "{0} is unavailable in {1}.";

    public string CatalogName { get; set; } = "the catalog";

    public override string FormatMessage(
        string format, string name) =>
        string.Format(CultureInfo.CurrentCulture,
            format, name, CatalogName);

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context,
        CancellationToken cancellationToken)
    {
        if (value is not string sku ||
            string.IsNullOrWhiteSpace(sku))
            return ValidationResult.Success;
        var catalog =
            context.GetRequiredService<CatalogAvailability>();
        if (await catalog.IsAvailableAsync(
                sku, cancellationToken))
            return ValidationResult.Success;
        string[] members = context.MemberName is null
            ? [] : [context.MemberName];
        return new ValidationResult(
            FormatErrorMessage(context.DisplayName), members);
    }

    protected override ValidationResult? IsValid(
        object? value, ValidationContext context) =>
        throw new InvalidOperationException(
            "Use the asynchronous validation path.");
}
