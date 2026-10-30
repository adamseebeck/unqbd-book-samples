using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace WarehouseApi.Configuration;

public static class RateLimitingConfiguration
{
    public const string OrderCreationPolicy = "OrderCreation";

    public static IServiceCollection AddWarehouseRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OrderRateLimitOptions>()
            .Bind(configuration.GetSection(OrderRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.AddPolicy(OrderCreationPolicy, context =>
            {
                // The options come from the request's services: no BuildServiceProvider call (ASP0000)
                var limits = context.RequestServices.GetRequiredService<IOptions<OrderRateLimitOptions>>().Value;

                // Each client gets its own budget: the token's subject, or the client address. Behind a
                // proxy, the forwarded headers middleware supplies the client's address
                var partitionKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = TimeSpan.FromSeconds(limits.WindowSeconds),

                    // Reject at once with Retry-After instead of holding the request open in a queue
                    QueueLimit = 0
                });
            });
        });

        return services;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        // The time until the current window ends, in whole seconds
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        // Problem Details like every other error; CustomizeProblemDetails adds the correlation ID
        var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Order submissions are limited per client. Retry after the time in the Retry-After header."
            }
        });
    }
}
