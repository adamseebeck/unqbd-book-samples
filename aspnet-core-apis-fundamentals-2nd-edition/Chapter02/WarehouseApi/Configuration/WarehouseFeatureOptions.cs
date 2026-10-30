using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Configuration;

public class WarehouseFeatureOptions
{
    public bool EnableRealTimeInventory { get; set; }

    [Range(1, 100)]
    public int DefaultPageSize { get; set; } = 20;

    [Range(1, 500)]
    public int MaxPageSize { get; set; } = 100;

    public string[] AllowedOrigins { get; set; } = [];
}
