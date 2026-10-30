using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Evaluation;
using ContosoSupportCopilot.Infrastructure.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// A quality gate: the whole pipeline runs offline over the sample dataset, so a change to chunking,
// retrieval, or the authorization rules that lowers these numbers fails the build, without an AI account.
// Offline answers quote their sources, so source overlap is always 1 here, and citation usefulness drops
// only for a question that retrieval misses, which recall already measures. Judging the answers needs a model.
public sealed class RagQualityTests : IAsyncLifetime
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"contoso-rag-quality-{Guid.NewGuid():N}");
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_folder);
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:Provider"] = "Offline",
            ["ConnectionStrings:SupportDb"] = $"Data Source={Path.Combine(_folder, "support.db")}",
            ["Evaluation:StoragePath"] = Path.Combine(_folder, "evaluation-results"),
            ["Rag:ProductAreasByEngineer:evaluation-runner:0"] = "Authentication",
            ["Rag:ProductAreasByEngineer:evaluation-runner:1"] = "Payments",
            ["Rag:ProductAreasByEngineer:evaluation-runner:2"] = "API",
            ["Rag:ProductAreasByEngineer:evaluation-runner:3"] = "Reporting",
            ["Logging:LogLevel:Default"] = "Warning"
        });

        // The same registrations as Program.cs, for the services the evaluation needs.
        builder.Services.AddSupportCopilotInfrastructure(builder.Configuration);
        builder.Services.AddSemanticKernelServices(builder.Configuration);
        builder.Services.AddVectorSearchServices(builder.Configuration);
        builder.Services.AddRagServices(builder.Configuration);
        builder.Services.AddEvaluationServices(builder.Configuration);
        _host = builder.Build();

        await _host.Services.InitializeSupportDatabaseAsync();
        await _host.Services.InitializeKnowledgeIndexAsync();
    }

    [Fact]
    public async Task SampleDataset_MeetsTheQualityBar_WithTheConfiguredRetrieval()
    {
        using var scope = _host.Services.CreateScope();
        var evaluation = scope.ServiceProvider.GetRequiredService<RagEvaluationService>();

        var report = await evaluation.EvaluateDatasetAsync(
            SampleEvaluationData.CreateDataset(),
            SupportPrompts.GroundedAnswer,
            new VectorSearchRequest { TopK = 5, MinScore = 0.2 },
            "quality-gate");

        Assert.False(evaluation.UsesJudges);
        Assert.True(report.AverageMetrics.Precision >= 0.8, $"Precision {report.AverageMetrics.Precision}");
        Assert.True(report.AverageMetrics.Recall >= 0.8, $"Recall {report.AverageMetrics.Recall}");
        Assert.True(report.AverageMetrics.CitationUsefulnessScore >= 0.8, $"Citation usefulness {report.AverageMetrics.CitationUsefulnessScore}");
        Assert.True(report.AverageMetrics.CitationsValid);
        Assert.Equal(RagOrchestrator.NoContentResponse, report.Results.Single(r => r.TestCaseName == "out-of-scope").Response);
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
        return Task.CompletedTask;
    }
}
