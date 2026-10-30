using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IFallbackIntentExtractor
{
    Task<SupportIntent> ExtractFallbackAsync(string ticketDescription, CancellationToken cancellationToken);
}
