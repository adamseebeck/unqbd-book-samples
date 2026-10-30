using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Health;

public class AIConnectivityValidator
{
    private readonly IChatClient _chatClient;
    private readonly AIProviderOptions _options;
    private readonly ILogger<AIConnectivityValidator> _logger;

    public AIConnectivityValidator(
        IChatClient chatClient,
        IOptions<AIProviderOptions> options,
        ILogger<AIConnectivityValidator> logger)
    {
        _chatClient = chatClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        if (_options.Provider == AIProvider.Offline)
        {
            _logger.LogInformation("AI provider is Offline: responses are local samples and no service is called");
            return true;
        }

        try
        {
            // One short request to the exact chat deployment the application uses.
            await _chatClient.GetResponseAsync("Reply with the single word OK.", cancellationToken: cancellationToken);
            _logger.LogInformation(
                "Connected to the {Provider} chat deployment {Deployment}",
                _options.Provider,
                _options.ChatDeployment);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Could not reach the {Provider} chat deployment {Deployment}",
                _options.Provider,
                _options.ChatDeployment);
            return false;
        }
    }
}
