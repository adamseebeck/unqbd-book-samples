using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

namespace AdventureWorks;

public static class StorePerformance
{
    public const string CatalogTag = "catalog";

    public static IServiceCollection AddStorePerformance(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Anonymous catalog reads, cached for a minute, tagged so
        // that a product write can evict them all at once.
        services.AddOutputCache(options =>
            options.AddPolicy(CatalogTag, policy => policy
                .Expire(TimeSpan.FromMinutes(1))
                .Tag(CatalogTag)));
        // One process here; Redis or SQL Server would share it.
        services.AddDistributedMemoryCache();
        services.AddRateLimiter(ConfigureRateLimits);
        services.AddResponseCompression(options =>
        {
            // HTTPS too, but Program.cs sends only the public
            // catalog here: it carries no secret (BREACH).
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.AddInventoryClient(configuration);
        return services;
    }

    // Five searches per ten seconds for each signed-in user, or
    // for each client address when nobody is signed in.
    static void ConfigureRateLimits(RateLimiterOptions options)
    {
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;
        options.AddPolicy("search", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.Identity?.IsAuthenticated == true
                    ? "user:" + context.User.Identity.Name
                    : "ip:" + context.Connection.RemoteIpAddress,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromSeconds(10)
                }));
        options.OnRejected = (rejected, _) =>
        {
            if (rejected.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var wait))
                rejected.HttpContext.Response.Headers.RetryAfter =
                    ((int)wait.TotalSeconds).ToString();
            return ValueTask.CompletedTask;
        };
    }
}
