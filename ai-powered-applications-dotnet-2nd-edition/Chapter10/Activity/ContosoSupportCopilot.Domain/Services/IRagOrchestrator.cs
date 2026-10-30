using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IRagOrchestrator
{
    Task<RagResult> GenerateGroundedResponseAsync(
        RagRequest request,
        SupportTicket ticket,
        CancellationToken cancellationToken = default);
}
