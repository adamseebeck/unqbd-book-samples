using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Configuration;

public class OrderRateLimitOptions
{
    public const string SectionName = "RateLimiting:OrderCreation";

    // Orders each client may submit per window
    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, 3_600)]
    public int WindowSeconds { get; set; } = 60;
}
