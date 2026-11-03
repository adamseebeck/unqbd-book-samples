using System.ComponentModel.DataAnnotations;

namespace AdventureWorks;

// What a caller may submit. Price and owner never come from here.
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
    [Required, RegularExpression(@"^[A-Z]{3}-[0-9]{4}$")]
    [AvailableSku]
    public string Sku { get; set; } = "";

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;
}

// New notes for an order, with the Version the client read.
public sealed class OrderNotesInput
{
    [StringLength(500)]
    public string? Notes { get; set; }

    public Guid Version { get; set; }
}
