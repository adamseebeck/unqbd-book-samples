using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace WarehouseApi.Configuration;

public static class ForwardedHeadersConfiguration
{
    public static IServiceCollection AddWarehouseForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // The proxy terminates TLS: X-Forwarded-Proto restores https, X-Forwarded-For the client address
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Only these senders may set the headers; anyone else could claim any address or scheme.
            // Loopback stays trusted, which covers a proxy on the same machine
            foreach (var network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }

            foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        return services;
    }
}
