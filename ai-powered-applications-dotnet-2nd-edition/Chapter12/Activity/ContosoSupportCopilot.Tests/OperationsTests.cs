using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Telemetry;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Correlation;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Extensions;
using ContosoSupportCopilot.Infrastructure.Health;
using ContosoSupportCopilot.Infrastructure.Monitoring;
using ContosoSupportCopilot.Infrastructure.Performance;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class OperationsTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly Mock<ITelemetryCollector> _telemetry = new();

    public OperationsTests() => _connection.Open();

    [Fact]
    public async Task DailyCostReport_PricesEveryModelCall_WithCachedInputAndEmbeddingsAtTheirRates()
    {
        await using var context = await CreateContextAsync(usage:
        [
            Usage(DateTime.UtcNow, ModelUsageRecord.Chat, input: 600_000, cached: 200_000, output: 100_000),
            Usage(DateTime.UtcNow, ModelUsageRecord.Chat, input: 400_000, cached: 0, output: 100_000),
            Usage(DateTime.UtcNow, ModelUsageRecord.Embeddings, input: 1_000_000, cached: 0, output: 0),
            Usage(DateTime.UtcNow.AddDays(-1), ModelUsageRecord.Chat, input: 9_000_000, cached: 0, output: 9_000_000)
        ]);
        var service = CreateService(context, dailyLimit: 2m);

        var report = await service.GetDailyCostReportAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        // 800,000 input tokens at $1.00, 200,000 cached at $0.25 and 200,000 output at $4.00 per million,
        // plus 1,000,000 embedding tokens at $0.02: 0.80 + 0.05 + 0.80 + 0.02.
        Assert.Equal(3, report.CallCount);
        Assert.Equal((1_000_000L, 200_000L, 200_000L, 1_000_000L),
            (report.InputTokens, report.CachedInputTokens, report.OutputTokens, report.EmbeddingTokens));
        Assert.Equal(1.67m, report.EstimatedCostUsd);
        Assert.True(report.AlertTriggered);
        Assert.False(report.BudgetExceeded);
        _telemetry.Verify(t => t.TrackEvent("budget_alert", It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Once);
    }

    [Fact]
    public async Task ConversationCost_FlagsAConversationAboveTheTokenLimit()
    {
        var conversationId = Guid.NewGuid();
        await using var context = await CreateContextAsync(runs: [Run(DateTime.UtcNow, 40_000, 20_000, conversationId)]);

        var report = await CreateService(context, dailyLimit: 100m).GetConversationCostAsync(conversationId);

        Assert.Equal(60_000, report.TotalTokens);
        Assert.True(report.OverTokenLimit);
    }

    [Fact]
    public async Task UsageRecorders_StoreEachCallsUsage_AndAFailedWriteNeverFailsTheCall()
    {
        await using var context = await CreateContextAsync();
        using var services = new ServiceCollection()
            .AddDbContext<CopilotDbContext>(options => options.UseSqlite(_connection))
            .BuildServiceProvider();
        var correlation = new CorrelationContext();
        correlation.SetCorrelationId("request-42");
        var recorder = new ModelUsageRecorder(services.GetRequiredService<IServiceScopeFactory>(), correlation, NullLogger<ModelUsageRecorder>.Instance);

        var chatModel = new Mock<IChatClient>();
        chatModel.Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "OK"))
            {
                ModelId = "chat-model",
                Usage = new UsageDetails { InputTokenCount = 120, CachedInputTokenCount = 20, OutputTokenCount = 30 }
            });
        var embeddingModel = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        embeddingModel.Setup(e => e.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[] { 1, 0 }) { ModelId = "embedding-model" }])
            {
                Usage = new UsageDetails { InputTokenCount = 9 }
            });

        await new UsageRecordingChatClient(chatModel.Object, recorder).GetResponseAsync("Which plan?");
        await new UsageRecordingEmbeddingGenerator(embeddingModel.Object, recorder).GenerateAsync(["reset link"]);

        // A caller that cancels once the answer has arrived still gets its tokens recorded: they are spent.
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await new UsageRecordingChatClient(chatModel.Object, recorder).GetResponseAsync("Which plan?", cancellationToken: cancelled.Token);

        var rows = await context.ModelUsage.OrderBy(u => u.Operation).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.Equal((ModelUsageRecord.Chat, "chat-model", 120L, 20L, 30L, "request-42"),
            (rows[0].Operation, rows[0].ModelId, rows[0].InputTokens, rows[0].CachedInputTokens, rows[0].OutputTokens, rows[0].CorrelationId));
        Assert.Equal(ModelUsageRecord.Chat, rows[1].Operation);
        Assert.Equal((ModelUsageRecord.Embeddings, "embedding-model", 9L), (rows[2].Operation, rows[2].ModelId, rows[2].InputTokens));

        // Without the table, the write fails; the caller still gets its answer.
        await context.Database.ExecuteSqlRawAsync("DROP TABLE ModelUsage");
        var response = await new UsageRecordingChatClient(chatModel.Object, recorder).GetResponseAsync("Which plan?");
        Assert.Equal("OK", response.Text);
    }

    [Fact]
    public async Task CachingEmbeddingClient_AnswersARepeatFromMemory_WithoutTokens()
    {
        var inner = new Mock<IEmbeddingClient>();
        inner.Setup(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<EmbeddingOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddingResult { Vector = [0.1f, 0.2f], TokenCount = 7, ModelId = "embedding-model" });
        using var cache = new CachingEmbeddingClient(inner.Object, _telemetry.Object, "embedding-model", maxEntries: 100);

        var first = await cache.GenerateAsync("reset link");
        var repeated = await cache.GenerateAsync("reset link");
        await cache.GenerateAsync("reset link", new EmbeddingOptions { Dimensions = 128 });

        Assert.Equal(7, first.TokenCount);
        Assert.Equal(0, repeated.TokenCount);
        inner.Verify(e => e.GenerateAsync(It.IsAny<string>(), It.IsAny<EmbeddingOptions?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _telemetry.Verify(t => t.TrackEmbeddingCache(true, 7), Times.Once);
    }

    [Fact]
    public void BenchmarkStatistics_UseTheNearestRank()
    {
        var stats = BenchmarkStatistics.Calculate([100, 10, 20, 30, 40, 50, 60, 70, 80, 90]);

        Assert.Equal((10, 50L, 100L, 100L), (stats.Count, stats.P50, stats.P95, stats.P99));
        Assert.Equal(new BenchmarkStatistics(1, 5, 5, 5), BenchmarkStatistics.Calculate([5]));
    }

    [Fact]
    public async Task Readiness_AfterAProbe_ChecksEveryDependency()
    {
        using var host = await AgentTestHost.StartAsync(services => services.AddProductionHealthChecks(new ConfigurationBuilder().Build()));

        Assert.Equal(AIProviderHealthMonitor.Healthy, await host.Services.GetRequiredService<AIProviderHealthMonitor>().ProbeAsync());
        var report = await host.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal("Healthy", report.Entries["dependencies"].Data["vector_store"]);
    }

    [Fact]
    public async Task Readiness_ReportsAFailingDependency_WithoutTheExceptionMessage()
    {
        var store = new Mock<IVectorSearchService>();
        store.Setup(s => s.SearchAsync(It.IsAny<float[]>(), It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("secret connection detail"));
        using var host = await AgentTestHost.StartAsync(services =>
        {
            services.AddProductionHealthChecks(new ConfigurationBuilder().Build());
            services.AddSingleton(store.Object);
        });

        var report = await host.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal("Unhealthy (InvalidOperationException)", report.Entries["dependencies"].Data["vector_store"]);
    }

    [Fact]
    public async Task Readiness_ReportsAnUnreachableAIProviderAsDegraded_WhichKeepsTheProbePassing()
    {
        using var host = await AgentTestHost.StartAsync(services =>
        {
            services.AddProductionHealthChecks(new ConfigurationBuilder().Build());
            services.AddSingleton(Validator(new UnreachableChatClient()));
        });

        Assert.Equal("Unhealthy", await host.Services.GetRequiredService<AIProviderHealthMonitor>().ProbeAsync());
        var report = await host.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        // ASP.NET Core answers 200 for Degraded and 503 only for Unhealthy.
        Assert.Equal(HealthStatus.Degraded, report.Status);
        Assert.Equal("Unhealthy", report.Entries["dependencies"].Data["ai_provider"]);
        Assert.Equal("Healthy", report.Entries["dependencies"].Data["copilot_database"]);
    }

    [Fact]
    public async Task ReadinessEndpoint_Answers200AtOnce_WhileTheAIProviderHangs()
    {
        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Health:AIProbeTimeoutSeconds"] = "120" })
            .Build();
        using var host = await AgentTestHost.StartAsync(services =>
        {
            services.AddProductionHealthChecks(settings);
            services.AddSingleton(Validator(new HangingChatClient()));
        });
        var endpoint = new ApplicationBuilder(host.Services)
            .UseHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
            .Build();

        // The first request builds the database models; the timed request follows.
        await RequestAsync(endpoint, host.Services, CancellationToken.None);
        var monitor = host.Services.GetRequiredService<AIProviderHealthMonitor>();
        await monitor.StartAsync(CancellationToken.None);
        try
        {
            // Like the kubelet with timeoutSeconds: 3, the request is aborted after 3 seconds.
            using var probeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var status = await RequestAsync(endpoint, host.Services, probeTimeout.Token);

            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(AIProviderHealthMonitor.Pending, monitor.Status);
        }
        finally
        {
            await monitor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Monitor_ReportsAProviderThatDoesNotAnswer_AsUnhealthyAfterTheProbeTimeout()
    {
        var monitor = new AIProviderHealthMonitor(Validator(new HangingChatClient()),
            Options.Create(new HealthOptions { AIProbeTimeoutSeconds = 1 }), NullLogger<AIProviderHealthMonitor>.Instance);

        Assert.Equal(AIProviderHealthMonitor.Pending, monitor.Status);
        Assert.Equal("Unhealthy (no answer in 1 s)", await monitor.ProbeAsync());
        Assert.Equal("Unhealthy (no answer in 1 s)", monitor.Status);
    }

    private static async Task<int> RequestAsync(RequestDelegate endpoint, IServiceProvider services, CancellationToken aborted)
    {
        var context = new DefaultHttpContext { RequestServices = services, RequestAborted = aborted };
        context.Request.Path = "/health/ready";
        context.Response.Body = new MemoryStream();
        await endpoint(context);
        return context.Response.StatusCode;
    }

    private static AIConnectivityValidator Validator(IChatClient chatClient) =>
        new(chatClient, Options.Create(new AIProviderOptions { Provider = AIProvider.Foundry, ChatDeployment = "chat" }),
            NullLogger<AIConnectivityValidator>.Instance);

    private CostMonitoringService CreateService(CopilotDbContext context, decimal dailyLimit) =>
        new(context, _telemetry.Object,
            Options.Create(new BudgetOptions { DailyLimitUsd = dailyLimit, AlertThresholdPercent = 80m, MaxTokensPerConversation = 50_000 }),
            Options.Create(new TokenPricingOptions { InputPerMillion = 1.00m, CachedInputPerMillion = 0.25m, OutputPerMillion = 4.00m, EmbeddingPerMillion = 0.02m }));

    private async Task<CopilotDbContext> CreateContextAsync(AgentRun[]? runs = null, ModelUsageRecord[]? usage = null)
    {
        var context = new CopilotDbContext(new DbContextOptionsBuilder<CopilotDbContext>().UseSqlite(_connection).Options);
        await context.Database.EnsureCreatedAsync();
        context.AgentRuns.AddRange(runs ?? []);
        context.ModelUsage.AddRange(usage ?? []);
        await context.SaveChangesAsync();
        return context;
    }

    private static AgentRun Run(DateTime timestamp, int input, int output, Guid? conversationId = null) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversationId ?? Guid.NewGuid(),
        AgentName = "KnowledgeAgent",
        ModelId = "chat-deployment",
        InputTokens = input,
        OutputTokens = output,
        Timestamp = timestamp
    };

    private static ModelUsageRecord Usage(DateTime timestamp, string operation, long input, long cached, long output) => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = timestamp,
        Operation = operation,
        ModelId = "model",
        InputTokens = input,
        CachedInputTokens = cached,
        OutputTokens = output
    };

    public void Dispose() => _connection.Dispose();

    private sealed class UnreachableChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("No route to the AI provider.");

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("No route to the AI provider.");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    // A provider that accepts the request and never answers, until the caller gives up.
    private sealed class HangingChatClient : IChatClient
    {
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
