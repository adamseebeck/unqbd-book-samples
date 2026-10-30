using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Integration;

public class ExternalShipmentServiceOptions
{
    public const string SectionName = "ExternalShipmentService";

    // Relative paths such as "api/shipments" are appended to the address, so it ends with "/"
    [Required, Url, RegularExpression(@"^https?://[^/]+(/.*)?/\z", ErrorMessage = "BaseUrl must be an http or https address that ends with /.")]
    public string BaseUrl { get; set; } = string.Empty;

    // A secret: user secrets in Development, an environment variable or a key vault elsewhere
    [Required(ErrorMessage = "Set ExternalShipmentService:ApiKey in user secrets or in the ExternalShipmentService__ApiKey environment variable.")]
    public string ApiKey { get; set; } = string.Empty;
}
