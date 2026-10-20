using System.ComponentModel.DataAnnotations;

public sealed record ProductDto(int Id, string Name, decimal Price, string Sku);

public sealed class CreateProductRequest : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Price { get; set; }
    [Required, RegularExpression(@"^[A-Z]{3}-[0-9]{4}$")]
    public string Sku { get; set; } = string.Empty;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Name.Contains("Test", StringComparison.OrdinalIgnoreCase) && Price > 1000)
            yield return new ValidationResult("Test products cannot exceed 1000.", [nameof(Price)]);
    }
}

public sealed class UpdateProductRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Price { get; set; }
}

public sealed record SearchParameters(string? Q, int Page = 1, int PageSize = 20);

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto[]> SearchAsync(string? query, int page, int pageSize);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<bool> UpdateAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteAsync(int id);
}
