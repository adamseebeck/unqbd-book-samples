using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Contracts;

// Nullable, so [Required] can detect a missing value; a plain enum would default to Pending
public record UpdateStatusRequest(
    [Required] OrderStatus? NewStatus
);
