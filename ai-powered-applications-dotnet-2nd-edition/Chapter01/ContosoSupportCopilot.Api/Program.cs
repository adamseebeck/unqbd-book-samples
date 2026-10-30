using ContosoSupportCopilot.Api;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Domain and application services
builder.Services.AddScoped<ISupportContext, InMemorySupportContext>();
builder.Services.AddScoped<SupportCopilotService>();

// Microsoft.Extensions.AI layer: one IChatClient for the whole application.
// The offline client answers locally; Chapter 2 adds the provider switch.
builder.Services.AddChatClient(new OfflineChatClient());
builder.Services.AddSingleton<ISupportChatClient, SupportChatClient>();

// Semantic Kernel orchestration layer
builder.Services.AddSingleton<IKernelOrchestrator, SemanticKernelOrchestrator>();

// Agent coordination layer
builder.Services.AddSingleton<IAgentCoordinator, AgentFrameworkCoordinator>();
builder.Services.AddSingleton(new TriageAgent("TriageAgent"));

var app = builder.Build();

// Register agents with the coordinator by name
var coordinator = app.Services.GetRequiredService<IAgentCoordinator>();
await coordinator.RegisterAgentAsync("TriageAgent", app.Services.GetRequiredService<TriageAgent>());

if (args.Contains("--check"))
{
    return await ChapterCheck.RunAsync(app.Services);
}

app.Run();
return 0;
