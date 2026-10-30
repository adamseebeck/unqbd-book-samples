// A local stand-in for the inventory service that Chapter 12's InventoryClient calls, so the retry, timeout
// and circuit-breaker steps can be run. It listens only on the loopback address the chapter's client uses.
// GET /stock/1 returns {"quantity":7} unless a control route says otherwise; every stock request is counted.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:42132");
var app = builder.Build();
var state = new FixtureState();

app.MapGet("/stock/{id:int}", async (int id, CancellationToken ct) =>
{
    var attempt = Interlocked.Increment(ref state.Attempts);
    app.Logger.LogInformation("Stock request {Attempt}", attempt);
    if (state.DelayMilliseconds > 0) await Task.Delay(state.DelayMilliseconds, ct);
    if (state.PermanentStatus is int status) return Results.StatusCode(status);
    if (Interlocked.Decrement(ref state.FailuresLeft) >= 0) return Results.StatusCode(500);
    Interlocked.Exchange(ref state.FailuresLeft, 0);
    return id == 1 ? Results.Ok(new { Quantity = 7 }) : Results.NotFound();
});
// Control routes: queue N failures (500), delay every response, return one status until reset, read or reset the count.
app.MapPost("/control/fail/{count:int}", (int count) => { state.FailuresLeft = Math.Clamp(count, 0, 1000); return Results.NoContent(); });
app.MapPost("/control/delay/{milliseconds:int}", (int milliseconds) => { state.DelayMilliseconds = Math.Clamp(milliseconds, 0, 30000); return Results.NoContent(); });
app.MapPost("/control/status/{code:int}", (int code) => { state.PermanentStatus = code is >= 400 and <= 599 ? code : null; return Results.NoContent(); });
app.MapGet("/control/attempts", () => Results.Ok(new { state.Attempts }));
app.MapPost("/control/reset", () => { state.Reset(); return Results.NoContent(); });
app.Run();

sealed class FixtureState
{
    public int Attempts, FailuresLeft, DelayMilliseconds;
    public int? PermanentStatus;
    public void Reset() { Attempts = 0; FailuresLeft = 0; DelayMilliseconds = 0; PermanentStatus = null; }
}
