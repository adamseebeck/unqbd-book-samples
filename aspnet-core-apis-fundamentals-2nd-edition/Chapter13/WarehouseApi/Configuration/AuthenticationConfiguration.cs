using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WarehouseApi.Security;

namespace WarehouseApi.Configuration;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddWarehouseAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // AddJwtBearer reads its settings from this section. In Development, dotnet user-jwts writes the
        // issuer and audiences to appsettings.Development.json and keeps the signing key in user secrets.
        var bearerSettings = configuration.GetSection("Authentication:Schemes:Bearer");

        var hasAudience = !string.IsNullOrEmpty(bearerSettings["ValidAudience"]) ||
            bearerSettings.GetSection("ValidAudiences").GetChildren().Any();

        // Elsewhere an identity provider issues the tokens; without it every request would fail later
        if (!environment.IsDevelopment() && (string.IsNullOrEmpty(bearerSettings["Authority"]) || !hasAudience))
        {
            throw new InvalidOperationException(
                "Set Authentication:Schemes:Bearer:Authority and an audience in Authentication:Schemes:Bearer:ValidAudiences, " +
                "for example with the Authentication__Schemes__Bearer__Authority and " +
                "Authentication__Schemes__Bearer__ValidAudiences__0 environment variables.");
        }

        services.AddSingleton<IClaimsTransformation, WarehouseClaimsTransformation>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Runs after the configuration section is applied: allow one minute of clock difference, not five
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(1);
            });

        return services;
    }
}
