using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IDraftingService
{
    IAsyncEnumerable<string> DraftReplyStreamingAsync(
        SupportTicket ticket,
        DraftingOptions options,
        CancellationToken cancellationToken = default);

    Task<DraftReply> DraftReplyAsync(
        SupportTicket ticket,
        DraftingOptions options,
        CancellationToken cancellationToken = default);
}
