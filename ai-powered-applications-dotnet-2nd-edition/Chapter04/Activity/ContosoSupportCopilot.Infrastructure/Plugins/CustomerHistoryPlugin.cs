using System.ComponentModel;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.SemanticKernel;

namespace ContosoSupportCopilot.Infrastructure.Plugins;

public class CustomerHistoryPlugin
{
    private readonly ISupportContext _supportContext;

    public CustomerHistoryPlugin(ISupportContext supportContext)
    {
        _supportContext = supportContext;
    }

    [KernelFunction("get_recent_ticket_count")]
    [Description("Retrieves recent support activity: the number of tickets the customer created in the last 30 days")]
    public Task<int> GetRecentTicketCountAsync(
        [Description("The unique identifier of the customer account")] Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return _supportContext.CountTicketsSinceAsync(customerId, DateTime.UtcNow.AddDays(-30), cancellationToken);
    }
}
