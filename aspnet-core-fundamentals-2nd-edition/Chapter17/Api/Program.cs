using System.Text;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.ObjectPool;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 65_536);
builder.Services.AddSingleton<ObjectPool<StringBuilder>>(_ =>
    new DefaultObjectPoolProvider().CreateStringBuilderPool(1024, 2048));
builder.Services.AddSingleton<BatchProcessor>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, PerformanceJsonContext.Default));
builder.Services.AddResponseCompression(o =>
{
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    // Local HTTP sample. Leave HTTPS compression disabled for secret-bearing responses.
    o.EnableForHttps = false;
});
var app = builder.Build();
app.UseResponseCompression();
app.MapPost("/api/ingestion/batch", async (HttpRequest request, BatchProcessor processor, CancellationToken ct) =>
{
    try { return Results.Ok(await processor.ProcessAsync(request.Body, ct)); }
    catch (BadHttpRequestException ex) when (ex.StatusCode == 413) { return Results.StatusCode(413); }
    catch (InvalidDataException) { return Results.BadRequest(); }
    catch (DecoderFallbackException) { return Results.BadRequest(); }
});
app.MapGet("/api/ingestion/sample", () => TypedResults.Ok(new BatchResult(1000, 3000)));
app.Run();
