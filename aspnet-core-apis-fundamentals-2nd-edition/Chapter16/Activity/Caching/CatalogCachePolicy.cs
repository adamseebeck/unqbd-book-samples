using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace WarehouseApi.Caching;

// The default policy never caches a response to an authenticated request, and every catalog request
// carries a token. The catalog is the same for every caller who may read it, and UseOutputCache runs
// after UseAuthorization, so a cached response only reaches callers that the endpoint's policy admits.
// Never use this policy for responses that differ by user, such as orders or one warehouse's stock
public sealed class CatalogCachePolicy : IOutputCachePolicy
{
    public const string Name = "ProductCatalog";
    public const string Tag = "products";

    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var request = context.HttpContext.Request;
        var cacheable = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);

        context.EnableOutputCaching = true;
        context.AllowCacheLookup = cacheable;
        context.AllowCacheStorage = cacheable;
        context.AllowLocking = true;
        context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(5);

        // The path, with its version segment, is always part of the key; add the search and paging values
        context.CacheVaryByRules.QueryKeys = "*";
        context.Tags.Add(Tag);

        return ValueTask.CompletedTask;
    }

    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // Store complete successes only, and never a response that sets a cookie
        var response = context.HttpContext.Response;
        if (response.StatusCode != StatusCodes.Status200OK || !StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = false;
        }

        return ValueTask.CompletedTask;
    }
}
