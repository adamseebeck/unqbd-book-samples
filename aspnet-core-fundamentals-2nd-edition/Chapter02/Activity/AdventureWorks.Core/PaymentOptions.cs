using System.ComponentModel.DataAnnotations;
namespace AdventureWorks.Core;
public sealed class PaymentOptions
{
    [Required, Url]
    public string ApiEndpoint { get; set; } = string.Empty;
    [Range(1, 60)]
    public int TimeoutSeconds { get; set; }
    [Range(0, 5)]
    public int RetryCount { get; set; }
}
