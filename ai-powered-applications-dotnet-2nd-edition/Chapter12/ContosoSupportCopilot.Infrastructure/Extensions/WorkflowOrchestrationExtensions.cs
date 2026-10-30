using ContosoSupportCopilot.Application.Workflows;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class WorkflowOrchestrationExtensions
{
    // Call after AddAgentServices: the workflow runs the agents through IAgentCoordinator.
    public static IServiceCollection AddWorkflowOrchestration(this IServiceCollection services, IConfiguration configuration)
    {
        var retries = configuration.GetValue("Workflow:MaxStepRetries", 2);

        // A transient stop often means an open circuit, so the wait is never shorter than the circuit breaker's
        // break (Chapter 4): a resume before the break ends meets the same open circuit and uses up a stop.
        var breakSeconds = configuration.GetValue("KernelResilience:CircuitBreakerBreakSeconds", 60);
        var retryAfter = Math.Max(configuration.GetValue("Workflow:RetryAfterSeconds", 60), breakSeconds);
        services.AddSingleton(new WorkflowRetryPolicy(Math.Clamp(retries, 0, 5), TimeSpan.FromSeconds(retryAfter)));

        // Scoped like the coordinator and the DbContext the state repository uses.
        services.AddScoped<IWorkflowStateRepository, SqlWorkflowStateRepository>();
        services.AddScoped<ISupportWorkflow, SupportTicketWorkflow>();

        return services;
    }
}
