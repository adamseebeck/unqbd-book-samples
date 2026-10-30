using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Services;

public interface ISupportChatClient
{
    Task<SupportChatResponse> CompleteAsync(string prompt, SupportChatOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SupportChatChunk> CompleteStreamingAsync(string prompt, SupportChatOptions? options = null, CancellationToken cancellationToken = default);
}
