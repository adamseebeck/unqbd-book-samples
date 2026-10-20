using System.ComponentModel.DataAnnotations;
namespace Chapter07Sample.Models;

public sealed class OrderFormInput
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
    [Range(1, 20)]
    public int Quantity { get; set; } = 1;
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

public sealed class OrderApiInput
{
    [Required, AvailableSku]
    public string Sku { get; set; } = "";
    [Range(1, 20)]
    public int Quantity { get; set; } = 1;
}
