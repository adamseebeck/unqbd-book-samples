using System.Globalization;
using System.Text.Json;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Telemetry;

public class FileTelemetryOptions
{
    public const string SectionName = "Telemetry:File";

    public string FilePath { get; set; } = "telemetry.jsonl";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    // Rotated files to keep; each rotation deletes older ones, so the folder never grows without bound.
    public int MaxRotatedFiles { get; set; } = 5;
}

// Writes each event as one JSON line, for local debugging without an OpenTelemetry pipeline. Every write
// waits for the file, under one lock, so it is no production exporter. A failed write is logged and dropped:
// IOException for a missing folder or a full disk, UnauthorizedAccessException for a folder the process may
// not write, such as /app for the non-root user of a chiseled image. Telemetry must never stop a workflow.
public sealed class FileTelemetryCollector : ITelemetryCollector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly FileTelemetryOptions _options;
    private readonly ILogger<FileTelemetryCollector> _logger;
    private readonly TimeProvider _time;
    private readonly Lock _gate = new();

    public FileTelemetryCollector(IOptions<FileTelemetryOptions> options, ILogger<FileTelemetryCollector> logger, TimeProvider? time = null)
    {
        _options = options.Value;
        _logger = logger;
        _time = time ?? TimeProvider.System;
    }

    public void TrackAgentRun(AgentRun run) => Write(new
    {
        Type = "agent_run",
        Timestamp = _time.GetUtcNow(),
        run.AgentName,
        run.ModelId,
        run.InputTokens,
        run.OutputTokens,
        run.LatencyMs,
        run.IsFallback,
        run.ConversationId,
        run.CorrelationId
    });

    public void TrackWorkflowStep(string stepName, Guid workflowId, long durationMs) =>
        Write(new { Type = "workflow_step", Timestamp = _time.GetUtcNow(), StepName = stepName, WorkflowId = workflowId, DurationMs = durationMs });

    public void TrackEmbeddingCache(bool hit, int tokensSaved) =>
        Write(new { Type = "embedding_cache", Timestamp = _time.GetUtcNow(), Hit = hit, TokensSaved = tokensSaved });

    public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties) =>
        Write(new { Type = eventName, Timestamp = _time.GetUtcNow(), Properties = properties });

    private void Write(object record)
    {
        var line = JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine;
        try
        {
            // One writer at a time, so lines from parallel requests never interleave.
            lock (_gate)
            {
                RotateIfFull();
                File.AppendAllText(_options.FilePath, line);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not write telemetry to {FilePath}", _options.FilePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Could not write telemetry to {FilePath}", _options.FilePath);
        }
    }

    private void RotateIfFull()
    {
        var file = new FileInfo(_options.FilePath);
        if (!file.Exists || file.Length < _options.MaxFileSizeBytes)
        {
            return;
        }

        var folder = file.DirectoryName ?? ".";
        var name = Path.GetFileNameWithoutExtension(file.Name);
        var extension = file.Extension;
        var stamp = _time.GetUtcNow().ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        file.MoveTo(Path.Combine(folder, $"{name}-{stamp}{extension}"));

        // The stamps sort by time: delete all but the newest MaxRotatedFiles.
        var rotated = Directory.GetFiles(folder, $"{name}-*{extension}").Order(StringComparer.Ordinal).ToList();
        foreach (var expired in rotated.Take(Math.Max(0, rotated.Count - _options.MaxRotatedFiles)))
        {
            File.Delete(expired);
        }
    }
}
