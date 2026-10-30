using System.Diagnostics;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Extensions;
using ContosoSupportCopilot.Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// An ActivityListener sees every span in the process, so these tests run alone.
[CollectionDefinition(nameof(TelemetryTests), DisableParallelization = true)]
public sealed class TelemetryCollection;

[Collection(nameof(TelemetryTests))]
public sealed class TelemetryTests
{
    [Fact]
    public async Task AgentRun_EmitsGenAISpans_WithUsageAndWithoutContent()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => TelemetryServiceExtensions.Sources.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => { lock (spans) { spans.Add(activity); } }
        };
        ActivitySource.AddActivityListener(listener);
        using var host = await AgentTestHost.StartAsync(services => services.AddContosoTelemetry(new ConfigurationBuilder().Build()));
        using var scope = host.Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<SupportCopilotService>().TriageTicketAsync(SampleData.PasswordTicketId);

        var agent = Assert.Single(spans, s => s.DisplayName.StartsWith("invoke_agent TriageAgent", StringComparison.Ordinal));
        Assert.Equal("invoke_agent", agent.GetTagItem("gen_ai.operation.name"));
        Assert.NotNull(agent.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Contains(spans, s => s.DisplayName == "execute_tool classify_ticket");

        // The run record's span gives its tokens contoso.* names, so it adds no third gen_ai.usage.* copy.
        var run = Assert.Single(spans, s => s.DisplayName == "agent_run TriageAgent");
        Assert.Equal("TriageAgent", run.GetTagItem("gen_ai.agent.name"));
        Assert.NotNull(run.GetTagItem("contoso.agent.input_tokens"));
        Assert.DoesNotContain(run.TagObjects, t => t.Key.StartsWith("gen_ai.usage.", StringComparison.Ordinal));

        // Content capture is off by default: no prompt or answer text in any span.
        Assert.DoesNotContain(spans, s => s.Tags.Any(t => t.Key is "gen_ai.input.messages" or "gen_ai.output.messages"));
    }

    [Fact]
    public async Task Workflow_RecordsEachStepThatEnded_AsAWorkflowStepSpan()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ActivityTelemetryCollector.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => { lock (spans) { spans.Add(activity); } }
        };
        ActivitySource.AddActivityListener(listener);
        using var host = await AgentTestHost.StartAsync(services =>
        {
            services.AddScoped<EscalationDraftService>();
            services.AddContosoTelemetry(new ConfigurationBuilder().Build());
        });
        using var scope = host.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
            .ExecuteAsync(SampleData.PasswordTicketId, "engineer-ana", "locked out");

        var steps = spans
            .Where(s => s.DisplayName.StartsWith("workflow_step ", StringComparison.Ordinal) && Equals(s.GetTagItem("contoso.workflow.id"), result.WorkflowId))
            .Select(s => s.GetTagItem("contoso.workflow.step"));
        Assert.Equal(["Triage", "KnowledgeRetrieval", "Drafting"], steps);
    }
}
