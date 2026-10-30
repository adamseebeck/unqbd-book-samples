using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Configuration;

public class DatabaseOptions
{
    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool EnableSensitiveDataLogging { get; set; }
}
