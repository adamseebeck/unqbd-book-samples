using Azure.Extensions.AspNetCore.Configuration.Secrets;
using ContosoSupportCopilot.Api;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Extensions;
using ContosoSupportCopilot.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configuration hierarchy: appsettings.json, appsettings.{Environment}.json, user secrets
// (Development), environment variables, command-line arguments, then Key Vault in Production.
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            AzureCredentialFactory.Create(builder.Environment),
            new AzureKeyVaultConfigurationOptions { ReloadInterval = TimeSpan.FromMinutes(30) });
    }
}

// Infrastructure: AI provider, database and agent coordination
builder.Services.AddSupportCopilotInfrastructure(builder.Configuration);

// Core AI services from Chapter 3: intent extraction and reply drafting
builder.Services.AddCoreAIServices();

// Semantic Kernel from Chapter 4: resilience pipeline, plugins and the orchestrator
builder.Services.AddSemanticKernelServices(builder.Configuration);

// Structured output and validation from Chapter 5
builder.Services.AddPromptValidationServices();

// Application services and agents from Chapters 1 and 4
builder.Services.AddScoped<SupportCopilotService>();
builder.Services.AddScoped<EscalationDraftService>();
builder.Services.AddSingleton(new TriageAgent("TriageAgent"));

// The AI check sends a model request, so it gets a time limit, and as a singleton it can
// reuse its last result.
builder.Services.AddSingleton<AIHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<AIHealthCheck>("ai_provider", timeout: TimeSpan.FromSeconds(10));

var app = builder.Build();

// Create the database on first run and add the sample data
await app.Services.InitializeSupportDatabaseAsync();

var coordinator = app.Services.GetRequiredService<IAgentCoordinator>();
await coordinator.RegisterAgentAsync("TriageAgent", app.Services.GetRequiredService<TriageAgent>());

if (args.Contains("--check"))
{
    // --check does not start the host, so it runs the host's startup validation itself.
    await app.Services.GetRequiredService<IAsyncStartupValidator>().ValidateAsync();
    return await ChapterCheck.RunAsync(app.Services);
}

// Liveness and readiness probes use /health/live, which runs no checks, so a provider outage
// never takes the API out of service. /health adds the AI provider check for monitoring.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health");

app.Run();
return 0;
