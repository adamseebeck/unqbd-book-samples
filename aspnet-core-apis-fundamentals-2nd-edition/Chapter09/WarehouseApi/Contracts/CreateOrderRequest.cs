using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

public record CreateOrderRequest(
    [Required]
    [StringLength(50)]
    string CustomerReference,

    [StringLength(10)]
    string? FulfillmentWarehouseCode,

    [Required]
    [MinLength(1)]
    IReadOnlyList<OrderLineRequest> Lines
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Required checks the collection; JSON can still put null inside it.
        if (Lines is null) yield break;

        for (var i = 0; i < Lines.Count; i++)
        {
            if (Lines[i] is null)
                yield return new ValidationResult("Each order line is required.", [$"Lines[{i}]"]);
        }
    }
}
