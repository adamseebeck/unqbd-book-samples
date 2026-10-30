using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IIntentExtractor
{
    Task<SupportIntent> ExtractAsync(
        string ticketDescription,
        CancellationToken cancellationToken = default);
}
