namespace WarehouseApi.Configuration;

public static class CorsConfiguration
{
    public static IServiceCollection AddWarehouseCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("IntegrationClients", policy =>
            {
                // Browser clients send the token in the Authorization header, so no AllowCredentials
                policy.WithOrigins(allowedOrigins)
                    .WithMethods("GET", "POST", "PUT", "DELETE")
                    .WithHeaders("Authorization", "Content-Type", "Idempotency-Key", "X-Correlation-ID")
                    // Scripts read only the headers listed here, including the version announcements
                    .WithExposedHeaders(
                        "X-Correlation-ID", "Location", "WWW-Authenticate",
                        "api-supported-versions", "api-deprecated-versions", "Deprecation", "Sunset", "Link")
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }
}
