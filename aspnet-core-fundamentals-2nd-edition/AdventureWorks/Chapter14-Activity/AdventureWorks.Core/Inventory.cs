using System.ComponentModel.DataAnnotations;

namespace AdventureWorks.Core;

public sealed record StockLevel(string Location, int Quantity);

// Move stock of one product between two of its locations.
public sealed class StockTransfer : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required, StringLength(50)]
    public string From { get; set; } = "";

    [Required, StringLength(50)]
    public string To { get; set; } = "";

    [Range(1, 1000)]
    public int Quantity { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (From == To)
            yield return new ValidationResult(
                "Use two different locations.", [nameof(To)]);
    }
}

public enum TransferOutcome { Moved, NotFound, NotEnoughStock }

public interface IInventoryRepository
{
    Task<StockLevel[]> GetAsync(int productId);
    Task<TransferOutcome> TransferAsync(StockTransfer transfer);
}
