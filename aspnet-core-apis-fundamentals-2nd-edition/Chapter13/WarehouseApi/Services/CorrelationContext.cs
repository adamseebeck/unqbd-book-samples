namespace WarehouseApi.Services;

public class CorrelationContext : ICorrelationContext
{
    private string _correlationId = Guid.NewGuid().ToString("N");

    public string CorrelationId => _correlationId;

    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));
        }

        _correlationId = correlationId;
    }
}
