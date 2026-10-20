// FeatureFlags.cs
using System.ComponentModel.DataAnnotations;
namespace AdventureWorks.Core;
public sealed class FeatureFlags
{
    public bool EnableNewCheckout { get; set; }
    [Range(1, 100)]
    public int MaxCartItems { get; set; }
    [Required, MinLength(1)]
    public string[] AllowedRegions { get; set; } = [];
}
