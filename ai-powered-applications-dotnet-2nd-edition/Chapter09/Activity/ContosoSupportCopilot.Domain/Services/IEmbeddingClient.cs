using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface IEmbeddingClient
{
    Task<EmbeddingResult> GenerateAsync(
        string text,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default);
}
