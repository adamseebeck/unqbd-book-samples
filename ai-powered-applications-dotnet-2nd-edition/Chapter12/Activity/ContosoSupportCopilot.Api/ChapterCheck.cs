using ContosoSupportCopilot.Domain.Telemetry;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this activity's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        var chat = services.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();
        Console.WriteLine($"Contoso Support Copilot: Chapter 12 activity check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");

        var collector = services.GetRequiredService<ITelemetryCollector>();
        var path = services.GetRequiredService<IOptions<FileTelemetryOptions>>().Value.FilePath;
        File.Delete(path);
        Console.WriteLine($"Telemetry collector: {collector.GetType().Name}, file {path}");

        await services.InitializeKnowledgeIndexAsync();
        using (var scope = services.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
                .ExecuteAsync(SampleData.PasswordTicketId, "engineer-ana", "Customer is locked out after a reset");
            Console.WriteLine($"Workflow: {result.FinalStep}");
        }

        var lines = File.ReadAllLines(path);
        var agentRuns = lines.Count(l => l.Contains("\"type\":\"agent_run\"", StringComparison.Ordinal));
        Console.WriteLine($"Telemetry file: {lines.Length} lines, {agentRuns} agent runs");
        Console.WriteLine($"First line: {lines.FirstOrDefault()?[..Math.Min(lines.FirstOrDefault()?.Length ?? 0, 160)]}");

        var passed = collector is FileTelemetryCollector && agentRuns == 3;
        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }
}
