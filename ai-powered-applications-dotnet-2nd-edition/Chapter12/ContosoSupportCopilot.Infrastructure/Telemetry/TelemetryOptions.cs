namespace ContosoSupportCopilot.Infrastructure.Telemetry;

public enum TelemetryExporter
{
    None,
    Console,
    Otlp
}

public class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ServiceName { get; set; } = "contoso-support-copilot";
    public string ServiceVersion { get; set; } = "1.0.0";

    // Otlp sends to OTEL_EXPORTER_OTLP_ENDPOINT, for example the Aspire dashboard or a collector.
    public TelemetryExporter Exporter { get; set; } = TelemetryExporter.None;

    // Prompts, answers and tool arguments in spans. Off by default: they can hold customer data.
    public bool EnableSensitiveData { get; set; }
}
