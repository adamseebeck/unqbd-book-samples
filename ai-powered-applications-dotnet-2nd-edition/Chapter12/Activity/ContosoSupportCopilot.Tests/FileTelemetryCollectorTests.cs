using System.Text.Json;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class FileTelemetryCollectorTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"contoso-telemetry-{Guid.NewGuid():N}");

    public FileTelemetryCollectorTests() => Directory.CreateDirectory(_folder);

    [Fact]
    public void TrackAgentRun_CreatesTheFile_WithOneCompleteJsonRecord()
    {
        var path = Path.Combine(_folder, "telemetry.jsonl");
        var collector = Create(path, maxBytes: 1024 * 1024);

        collector.TrackAgentRun(new AgentRun { AgentName = "TriageAgent", ModelId = "chat-deployment", InputTokens = 120, OutputTokens = 30, LatencyMs = 450 });

        var record = JsonDocument.Parse(Assert.Single(File.ReadAllLines(path))).RootElement;
        Assert.Equal("agent_run", record.GetProperty("type").GetString());
        Assert.Equal("TriageAgent", record.GetProperty("agentName").GetString());
        Assert.Equal("chat-deployment", record.GetProperty("modelId").GetString());
        Assert.Equal(120, record.GetProperty("inputTokens").GetInt32());
        Assert.Equal(30, record.GetProperty("outputTokens").GetInt32());
        Assert.Equal(450, record.GetProperty("latencyMs").GetInt32());
        Assert.True(record.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public void Write_RotatesTheFile_WhenItReachesTheLimit()
    {
        var path = Path.Combine(_folder, "telemetry.jsonl");
        var collector = Create(path, maxBytes: 100);

        collector.TrackAgentRun(new AgentRun { AgentName = "TriageAgent" });
        collector.TrackAgentRun(new AgentRun { AgentName = "KnowledgeAgent" });

        Assert.Single(Directory.GetFiles(_folder, "telemetry-*.jsonl"));
        Assert.Contains("KnowledgeAgent", File.ReadAllText(path));
    }

    [Fact]
    public void Write_KeepsOnlyTheNewestRotatedFiles()
    {
        var path = Path.Combine(_folder, "telemetry.jsonl");
        var collector = new FileTelemetryCollector(
            Options.Create(new FileTelemetryOptions { FilePath = path, MaxFileSizeBytes = 100, MaxRotatedFiles = 2 }),
            NullLogger<FileTelemetryCollector>.Instance, new SteppingTime());

        // Every record is larger than the limit, so writes 2 to 5 each rotate the file before they append.
        foreach (var agent in new[] { "A", "B", "C", "D", "E" })
        {
            collector.TrackAgentRun(new AgentRun { AgentName = $"Agent{agent}" });
        }

        var kept = Directory.GetFiles(_folder, "telemetry-*.jsonl").Order(StringComparer.Ordinal).Select(File.ReadAllText).ToList();
        Assert.Equal(2, kept.Count);
        Assert.Contains("AgentC", kept[0]);
        Assert.Contains("AgentD", kept[1]);
        Assert.Contains("AgentE", File.ReadAllText(path));
    }

    [Fact]
    public void Write_ToAMissingFolder_LogsInsteadOfThrowing()
    {
        var logger = new Mock<ILogger<FileTelemetryCollector>>();
        var collector = new FileTelemetryCollector(
            Options.Create(new FileTelemetryOptions { FilePath = Path.Combine(_folder, "missing", "telemetry.jsonl") }), logger.Object);

        collector.TrackEvent("budget_alert", new Dictionary<string, object?> { ["contoso.budget.cost_usd"] = 12.5 });

        logger.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<DirectoryNotFoundException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private static FileTelemetryCollector Create(string path, long maxBytes) =>
        new(Options.Create(new FileTelemetryOptions { FilePath = path, MaxFileSizeBytes = maxBytes }), NullLogger<FileTelemetryCollector>.Instance);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A clock that moves one second per reading, so every rotation gets its own stamp.
    private sealed class SteppingTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now = _now.AddSeconds(1);
    }
}
