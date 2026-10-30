namespace WarehouseApi.Services;

public interface ICorrelationContext
{
    string CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}
