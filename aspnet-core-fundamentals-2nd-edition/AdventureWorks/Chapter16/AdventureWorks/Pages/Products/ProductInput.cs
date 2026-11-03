using System.ComponentModel.DataAnnotations;

namespace AdventureWorks.Pages.Products;

public sealed class ProductInput
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue)]
    public int Version { get; set; }
}
