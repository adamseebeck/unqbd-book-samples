using System.Diagnostics;

namespace WarehouseApi.Telemetry;

public static class WarehouseTelemetry
{
    public const string SourceName = "Warehouse.Inventory";

    // Its activities become spans only when a listener, such as OpenTelemetry, subscribes to the source
    public static readonly ActivitySource Source = new(SourceName);

    public static class ActivityNames
    {
        public const string ReserveStock = "ReserveStock";
        public const string QueryStock = "QueryStock";
        public const string RegisterShipment = "RegisterShipment";
    }

    public static class Tags
    {
        public const string OrderId = "warehouse.order_id";
        public const string Warehouse = "warehouse.code";
        public const string RetryCount = "warehouse.retry_count";
        public const string ResultCount = "warehouse.result_count";
        public const string CorrelationId = "warehouse.correlation_id";
    }
}
