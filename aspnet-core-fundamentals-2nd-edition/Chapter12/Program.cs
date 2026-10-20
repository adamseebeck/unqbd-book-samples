using System.IO.Compression;
using System.Security.Claims;
using System.Threading.RateLimiting;
using AdventureWorks.CacheSample;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Http.Resilience;
using Polly.CircuitBreaker;
using Polly.Timeout;
var builder = WebApplication.CreateBuilder(args);
var redis = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redis)) builder.Services.AddDistributedMemoryCache();
else builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redis;
    options.InstanceName = "adventureworks-ch12:";
});
builder.Services.AddSingleton<ProductSource>();
builder.Services.AddSingleton<CatalogCache>();
builder.Services.AddOutputCache(options =>
{
    options.SizeLimit = 8 * 1024 * 1024;
    options.MaximumBodySize = 64 * 1024;
    options.AddPolicy("catalog", policy => policy.Expire(TimeSpan.FromSeconds(30)).Tag("products"));
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local-unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 3, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("per-user", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddTokenBucketLimiter("webhook", o =>
    { o.TokenLimit = 20; o.TokensPerPeriod = 10; o.ReplenishmentPeriod = TimeSpan.FromMinutes(1); o.QueueLimit = 0; });
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = false;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.AddHttpClient<InventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Inventory:BaseUrl"] ?? "http://127.0.0.1:42132/");
    client.Timeout = Timeout.InfiniteTimeSpan;
}).AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.Delay = TimeSpan.FromMilliseconds(100);
    options.Retry.DisableForUnsafeHttpMethods();
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
});
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseResponseCompression();
app.UseRateLimiter(); // Count requests even when output cache serves a hit.
app.UseOutputCache();
app.MapGet("/api/products/{id:int}", async (int id, CatalogCache catalog, CancellationToken ct) =>
{
    var product = await catalog.GetAsync(id, ct);
    return product is null ? Results.NotFound() : Results.Ok(product);
}).CacheOutput("catalog");
app.MapGet("/limited", () => Results.Ok("Allowed")).RequireRateLimiting("public");
app.MapGet("/profile", (ClaimsPrincipal user) => Results.Ok(new { Name = user.Identity?.Name }))
    .RequireAuthorization().RequireRateLimiting("per-user").CacheOutput();
app.MapGet("/api/stock", async (InventoryClient inventory, CancellationToken ct) =>
{
    try { return Results.Ok(await inventory.GetAsync(ct)); }
    catch (HttpRequestException) { return Results.StatusCode(503); }
    catch (BrokenCircuitException) { return Results.StatusCode(503); }
    catch (TimeoutRejectedException) { return Results.StatusCode(503); }
});
app.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
    Results.Ok(new { Token = antiforgery.GetAndStoreTokens(context).RequestToken })).RequireAuthorization();
app.MapPost("/api/inventory/webhook", async (HttpContext context, IAntiforgery antiforgery,
    CatalogCache catalog, IOutputCacheStore output, CancellationToken ct) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    // Training invalidation only. An authenticated upstream owns durable stock updates.
    await catalog.InvalidateAsync(ct);
    await output.EvictByTagAsync("products", ct);
    return Results.NoContent();
}).RequireAuthorization(policy => policy.RequireRole("Admin")).RequireRateLimiting("webhook");
app.Run();
