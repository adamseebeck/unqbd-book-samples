namespace WarehouseApi.Configuration;

public static class ApiVersionConstants
{
    // Asp.Versioning accepts a double for a major.minor version, so the same constants work in
    // [ApiVersion], [MapToApiVersion], the Minimal API methods and the deprecation and sunset policies
    public const double V1 = 1.0;
    public const double V2 = 2.0;

    // OpenAPI document names; they match the API explorer group names that GroupNameFormat "'v'VVV" produces
    public const string V1DocumentName = "v1";
    public const string V2DocumentName = "v2";
}
