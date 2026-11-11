using System.Collections.Frozen;

// A stand-in for the warehouse's stock service, so Chapter 12 can
// test caching and resilience without a real one. It listens on
// 127.0.0.1:42132 only (Properties/launchSettings.json), and its
// /control routes make the next calls fail, wait or answer with
// another status.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var stock = new Dictionary<string, int>
{
    ["HLM-1001"] = 42, ["JRS-2001"] = 17,
    ["BIK-3001"] = 3, ["TNT-4001"] = 8
}.ToFrozenDictionary();
var faults = new Faults();

app.MapGet("/stock/{sku}", async (string sku,
    CancellationToken cancellationToken) =>
{
    Interlocked.Increment(ref faults.Attempts);
    if (faults.DelayMs > 0)
        await Task.Delay(faults.DelayMs, cancellationToken);
    if (Interlocked.Decrement(ref faults.FailuresLeft) >= 0)
        return Results.StatusCode(faults.Status);
    return stock.TryGetValue(sku, out var available)
        ? Results.Ok(new { Sku = sku, Available = available })
        : Results.NotFound();
});

var control = app.MapGroup("/control");
control.MapPost("/fail/{count:int}", (int count) =>
{
    faults.FailuresLeft = count;
    return Results.NoContent();
});
control.MapPost("/status/{code:int}", (int code) =>
{
    faults.Status = code;
    return Results.NoContent();
});
control.MapPost("/delay/{ms:int}", (int ms) =>
{
    faults.DelayMs = ms;
    return Results.NoContent();
});
control.MapGet("/attempts", () => new { faults.Attempts });
control.MapPost("/reset", () =>
{
    faults = new Faults();
    return Results.NoContent();
});

app.Run();
