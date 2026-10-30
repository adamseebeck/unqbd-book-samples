using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContosoSupportCopilot.Tests;

// The agent registrations from Program.cs on the offline provider, with temporary SQLite files.
// A test can replace any service through configureServices, which runs after the registrations.
public sealed class AgentTestHost : IDisposable
{
    private readonly string _folder;

    private AgentTestHost(IHost host, string folder)
    {
        Services = host.Services;
        _host = host;
        _folder = folder;
    }

    private readonly IHost _host;

    public IServiceProvider Services { get; }

    public static async Task<AgentTestHost> StartAsync(Action<IServiceCollection>? configureServices = null)
    {
        var folder = Path.Combine(Path.GetTempPath(), $"contoso-agents-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:Provider"] = "Offline",
            ["ConnectionStrings:SupportDb"] = $"Data Source={Path.Combine(folder, "support.db")}",
            ["ConnectionStrings:CopilotDb"] = $"Data Source={Path.Combine(folder, "copilot.db")}",
            ["Rag:ProductAreasByEngineer:engineer-ana:0"] = "Authentication",
            ["Rag:ProductAreasByEngineer:engineer-ana:1"] = "API",
            ["Rag:ProductAreasByEngineer:engineer-ben:0"] = "Payments",
            ["Logging:LogLevel:Default"] = "Warning"
        });

        builder.Services.AddSupportCopilotInfrastructure(builder.Configuration);
        builder.Services.AddCoreAIServices();
        builder.Services.AddSemanticKernelServices(builder.Configuration);
        builder.Services.AddPromptValidationServices();
        builder.Services.AddWorkflowServices(builder.Configuration);
        builder.Services.AddVectorSearchServices(builder.Configuration);
        builder.Services.AddRagServices(builder.Configuration);
        builder.Services.AddAgentServices();
        builder.Services.AddScoped<SupportCopilotService>();
        configureServices?.Invoke(builder.Services);

        var host = builder.Build();
        await host.Services.InitializeSupportDatabaseAsync();
        await host.Services.InitializeCopilotDatabaseAsync();
        await host.Services.InitializeKnowledgeIndexAsync();
        return new AgentTestHost(host, folder);
    }

    public void Dispose()
    {
        _host.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }
}
