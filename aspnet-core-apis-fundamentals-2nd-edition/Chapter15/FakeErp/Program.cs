using FakeErp;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// FailFirstRequests, LoseFirstResponses and DelayMilliseconds make this ERP slow or unreliable on purpose
builder.Services.Configure<FakeErpOptions>(builder.Configuration.GetSection(FakeErpOptions.SectionName));
builder.Services.AddSingleton<ShipmentLedger>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapPost("/api/shipments", async (
    ShipmentRequest request,
    [FromHeader(Name = "X-API-Key")] string? apiKey,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    ShipmentLedger ledger,
    CancellationToken cancellationToken) =>
{
    // A real ERP would check the key; this one only insists that the client sends one
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Problem("The X-API-Key header is required.", statusCode: StatusCodes.Status401Unauthorized);
    }

    if (string.IsNullOrEmpty(idempotencyKey))
    {
        return Results.Problem("The Idempotency-Key header is required.", statusCode: StatusCodes.Status400BadRequest);
    }

    var (outcome, confirmation) = await ledger.RecordAsync(request, idempotencyKey, cancellationToken);
    return outcome switch
    {
        LedgerOutcome.Created => Results.Created($"/api/shipments/{confirmation!.ShipmentId}", confirmation),
        LedgerOutcome.Replayed => Results.Ok(confirmation),
        LedgerOutcome.KeyReused => Results.Problem(
            "The Idempotency-Key was already used for another order.", statusCode: StatusCodes.Status409Conflict),
        _ => Results.Problem("The ERP is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable)
    };
});

// The shipments created so far and the number of shipment requests that arrived
app.MapGet("/api/shipments", (ShipmentLedger ledger) => Results.Ok(ledger.GetSummary()));

// One shipment, the address that the Location header of a 201 response names
app.MapGet("/api/shipments/{shipmentId}", (string shipmentId, ShipmentLedger ledger) =>
    ledger.Find(shipmentId) is { } shipment ? Results.Ok(shipment) : Results.NotFound());

// The next N shipment requests fail with 503
app.MapPost("/fake/outage", (int requests, ShipmentLedger ledger) =>
{
    ledger.StartOutage(requests);
    return Results.NoContent();
});

// Unavailable while an outage lasts
app.MapMethods("/health", ["GET", "HEAD"], (ShipmentLedger ledger) =>
    ledger.IsUnavailable
        ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
        : Results.Text("Healthy"));

app.Run();
