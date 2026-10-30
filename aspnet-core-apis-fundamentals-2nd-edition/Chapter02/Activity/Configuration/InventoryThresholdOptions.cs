using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Configuration;

public class InventoryThresholdOptions
{
    [Range(1, 10_000)]
    public int LowStockThreshold { get; set; } = 20;

    [Range(0, 10_000)]
    public int CriticalStockThreshold { get; set; } = 5;

    // Supplied by user secrets in Development and by an environment variable in production
    [Required, EmailAddress]
    public string NotificationEmail { get; set; } = string.Empty;
}
