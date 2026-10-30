using System.ClientModel;
using System.ClientModel.Primitives;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class AIClientFactory
{
    private const string FoundryScope = "https://ai.azure.com/.default";
    private static readonly Uri DefaultOllamaEndpoint = new("http://localhost:11434");

    private readonly AIProviderOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IReadOnlyList<IOfflineResponder> _offlineResponders;

    // Chapter 8 registers offline responders, such as the grounded answer with citations.
    public AIClientFactory(IOptions<AIProviderOptions> options, IHostEnvironment environment, IEnumerable<IOfflineResponder> offlineResponders)
    {
        _options = options.Value;
        _environment = environment;
        _offlineResponders = offlineResponders.ToList();
    }

    // Chapter 9 passes a deployment for the evaluation judges; empty uses AI:ChatDeployment.
    public IChatClient CreateChatClient(string? deployment = null)
    {
        var chatDeployment = string.IsNullOrWhiteSpace(deployment) ? _options.ChatDeployment : deployment;
        return _options.Provider switch
        {
            AIProvider.Offline => new OfflineChatClient(_offlineResponders),
            AIProvider.Ollama => CreateOllamaClient(Require(chatDeployment, "AI:ChatDeployment")),
            _ => CreateOpenAIClient().GetChatClient(Require(chatDeployment, "AI:ChatDeployment")).AsIChatClient()
        };
    }

    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator() => _options.Provider switch
    {
        AIProvider.Offline => new OfflineEmbeddingGenerator(),
        AIProvider.Ollama => CreateOllamaClient(Require(_options.EmbeddingDeployment, "AI:EmbeddingDeployment")),
        _ => CreateOpenAIClient().GetEmbeddingClient(Require(_options.EmbeddingDeployment, "AI:EmbeddingDeployment")).AsIEmbeddingGenerator()
    };

    private OllamaApiClient CreateOllamaClient(string model)
    {
        var endpoint = string.IsNullOrWhiteSpace(_options.Endpoint) ? DefaultOllamaEndpoint : new Uri(_options.Endpoint);
        var httpClient = new HttpClient { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds) };
        return new OllamaApiClient(httpClient, model);
    }

    private OpenAIClient CreateOpenAIClient()
    {
        var clientOptions = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
            // From Chapter 4 the resilience pipeline owns every retry, so the SDK sends each request once.
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0)
        };
        if (!string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            // Foundry: https://<resource>.openai.azure.com/openai/v1/ (OpenAI uses its default endpoint)
            clientOptions.Endpoint = new Uri(_options.Endpoint.TrimEnd('/') + "/");
        }

        if (_options.Provider == AIProvider.Foundry && _options.UseEntraId)
        {
            #pragma warning disable OPENAI001 // The OpenAI SDK marks token-based authentication as experimental.
            var tokenPolicy = new BearerTokenPolicy(AzureCredentialFactory.Create(_environment), FoundryScope);
            return new OpenAIClient(tokenPolicy, clientOptions);
            #pragma warning restore OPENAI001
        }

        return new OpenAIClient(new ApiKeyCredential(Require(_options.ApiKey, "AI:ApiKey")), clientOptions);
    }

    private static string Require(string? value, string key) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{key} is not configured.") : value;
}
