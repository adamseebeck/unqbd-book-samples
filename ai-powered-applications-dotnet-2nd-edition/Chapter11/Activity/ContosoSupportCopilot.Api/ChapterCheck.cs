using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Extensions.AI;

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
        Console.WriteLine($"Contoso Support Copilot: Chapter 11 activity check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        var key = $"check-{Guid.NewGuid():N}";

        var first = await ProcessAsync(services, key);
        var runsAfterFirst = await CountRunsAsync(services);
        var repeated = await ProcessAsync(services, key);
        var runsAfterRepeat = await CountRunsAsync(services);
        var withoutKey = await ProcessAsync(services, null);
        var runsAfterNew = await CountRunsAsync(services);

        Console.WriteLine($"First request: {first.FinalStep}, workflow {Short(first.WorkflowId)}; agent runs {runsAfterFirst}");
        Console.WriteLine($"Same key again: workflow {Short(repeated.WorkflowId)}, same workflow {repeated.WorkflowId == first.WorkflowId}; agent runs {runsAfterRepeat}");
        Console.WriteLine($"No key: new workflow {repeated.WorkflowId != withoutKey.WorkflowId}; agent runs {runsAfterNew}");

        // Keys belong to the engineer who sent them: the same key from engineer-ben starts his own run.
        var otherEngineer = await ProcessAsync(services, key, "engineer-ben");
        Console.WriteLine($"Same key from engineer-ben: new workflow {otherEngineer.WorkflowId != first.WorkflowId}, escalation {otherEngineer.EscalationRequired}");

        var passed = repeated.WorkflowId == first.WorkflowId && runsAfterRepeat == runsAfterFirst
            && withoutKey.WorkflowId != first.WorkflowId && runsAfterNew > runsAfterRepeat
            && otherEngineer.WorkflowId != first.WorkflowId;
        Console.WriteLine(passed ? "Check passed." : "Check failed.");
        return passed ? 0 : 1;
    }

    private static async Task<WorkflowResult> ProcessAsync(IServiceProvider services, string? idempotencyKey, string engineerId = "engineer-ana")
    {
        using var scope = services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISupportWorkflow>()
            .ExecuteAsync(SampleData.PasswordTicketId, engineerId, "Customer is locked out after a reset", idempotencyKey);
    }

    private static async Task<int> CountRunsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var conversation = await scope.ServiceProvider.GetRequiredService<IConversationRepository>().GetByTicketAsync(SampleData.PasswordTicketId);
        return conversation is null ? 0 : (await scope.ServiceProvider.GetRequiredService<IAgentRunRepository>().GetByConversationAsync(conversation.Id)).Count;
    }

    private static string Short(Guid id) => id.ToString()[..8];
}
