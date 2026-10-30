using Azure.Extensions.AspNetCore.Configuration.Secrets;
using ContosoSupportCopilot.Api;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Agents;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Extensions;
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

// Infrastructure: AI provider, database, Semantic Kernel and agent coordination
builder.Services.AddSupportCopilotInfrastructure(builder.Configuration);

// Application services and agents from Chapter 1
builder.Services.AddScoped<SupportCopilotService>();
builder.Services.AddSingleton(new TriageAgent("TriageAgent"));

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

// Probes use /health/live; /health returns the JSON report with the AI provider check.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });

app.Run();
return 0;
