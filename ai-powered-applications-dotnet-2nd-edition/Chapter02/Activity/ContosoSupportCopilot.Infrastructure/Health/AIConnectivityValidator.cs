using System.Diagnostics;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Health;

public record ConnectivityResult(bool IsConnected, string Provider, string? ChatDeployment, string Message, long ElapsedMilliseconds);

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

    public async Task<ConnectivityResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var provider = _options.Provider.ToString();
        if (_options.Provider == AIProvider.Offline)
        {
            _logger.LogInformation("AI provider is Offline: responses are local samples and no service is called");
            return new ConnectivityResult(true, provider, null, "Offline provider: responses are local samples", 0);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _chatClient.GetResponseAsync("Reply with the single word OK.", cancellationToken: cancellationToken);
            _logger.LogInformation(
                "Connected to the {Provider} chat deployment {Deployment}",
                _options.Provider,
                _options.ChatDeployment);
            return new ConnectivityResult(true, provider, _options.ChatDeployment, "Chat deployment reachable", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Could not reach the {Provider} chat deployment {Deployment}",
                _options.Provider,
                _options.ChatDeployment);
            return new ConnectivityResult(false, provider, _options.ChatDeployment, ex.GetType().Name, stopwatch.ElapsedMilliseconds);
        }
    }
}
