using System.Diagnostics;
using ContosoSupportCopilot.Domain.Telemetry;
using ContosoSupportCopilot.Domain.Workflows;

namespace ContosoSupportCopilot.Infrastructure.Performance;

public record BenchmarkResult(Guid WorkflowId, long DurationMs, bool Success, WorkflowStep FinalStep);

public record BenchmarkStatistics(int Count, long P50, long P95, long P99)
{
    public static BenchmarkStatistics Calculate(IEnumerable<long> durations)
    {
        var sorted = durations.Order().ToList();
        return sorted.Count == 0
            ? new BenchmarkStatistics(0, 0, 0, 0)
            : new BenchmarkStatistics(sorted.Count, Percentile(sorted, 0.50), Percentile(sorted, 0.95), Percentile(sorted, 0.99));
    }

    // Nearest rank: the smallest value with at least p of the samples at or below it.
    private static long Percentile(List<long> sorted, double p) =>
        sorted[Math.Max(0, (int)Math.Ceiling(p * sorted.Count) - 1)];
}

// End-to-end latency of the multi-agent workflow, measured in a test or before a release.
public class WorkflowBenchmark
{
    private readonly ISupportWorkflow _workflow;
    private readonly ITelemetryCollector _telemetry;
    private readonly List<BenchmarkResult> _results = [];

    public WorkflowBenchmark(ISupportWorkflow workflow, ITelemetryCollector telemetry)
    {
        _workflow = workflow;
        _telemetry = telemetry;
    }

    public async Task<BenchmarkResult> RunIterationAsync(Guid ticketId, string engineerId, string input, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _workflow.ExecuteAsync(ticketId, engineerId, input, cancellationToken);
        stopwatch.Stop();

        var benchmark = new BenchmarkResult(result.WorkflowId, stopwatch.ElapsedMilliseconds, result.IsSuccess, result.FinalStep);
        _results.Add(benchmark);

        // An event of its own: the workflow step histogram keeps only real steps.
        _telemetry.TrackEvent("benchmark_iteration", new Dictionary<string, object?>
        {
            ["contoso.workflow.id"] = result.WorkflowId,
            ["contoso.workflow.final_step"] = result.FinalStep.ToString(),
            ["contoso.benchmark.duration_ms"] = stopwatch.ElapsedMilliseconds
        });
        return benchmark;
    }

    public BenchmarkStatistics GetStatistics() => BenchmarkStatistics.Calculate(_results.Select(r => r.DurationMs));
}
