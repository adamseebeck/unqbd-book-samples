using WarehouseApi.Services;

namespace WarehouseApi.Integration;

// A typed client: IHttpClientFactory supplies the configured HttpClient
public class ExternalShipmentService : IExternalShipmentService
{
    private readonly HttpClient _httpClient;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<ExternalShipmentService> _logger;

    public ExternalShipmentService(
        HttpClient httpClient,
        ICorrelationContext correlationContext,
        ILogger<ExternalShipmentService> logger)
    {
        _httpClient = httpClient;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task<ShipmentConfirmation> CreateShipmentAsync(
        Guid orderId,
        string warehouseCode,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/shipments")
        {
            Content = JsonContent.Create(new { OrderId = orderId, WarehouseCode = warehouseCode })
        };

        // The same key on every attempt for this order: the ERP answers a repeated request
        // with the shipment it already created instead of creating a second one
        request.Headers.Add("Idempotency-Key", $"order-{orderId}");

        // The job's correlation context holds the ID of the request that allocated the order
        request.Headers.Add("X-Correlation-ID", _correlationContext.CorrelationId);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var confirmation = await response.Content.ReadFromJsonAsync<ShipmentConfirmation>(cancellationToken)
            ?? throw new InvalidOperationException($"The ERP returned no shipment for order {orderId}.");

        _logger.LogInformation(
            "The ERP confirmed shipment {ShipmentId} for order {OrderId} ({StatusCode})",
            confirmation.ShipmentId, orderId, (int)response.StatusCode);

        return confirmation;
    }
}
