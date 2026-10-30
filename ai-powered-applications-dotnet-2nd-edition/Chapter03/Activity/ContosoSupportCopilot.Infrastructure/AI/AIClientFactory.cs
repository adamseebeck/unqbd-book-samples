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

    public AIClientFactory(IOptions<AIProviderOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public IChatClient CreateChatClient() => _options.Provider switch
    {
        AIProvider.Offline => new OfflineChatClient(),
        AIProvider.Ollama => CreateOllamaClient(Require(_options.ChatDeployment, "AI:ChatDeployment")),
        _ => CreateOpenAIClient().GetChatClient(Require(_options.ChatDeployment, "AI:ChatDeployment")).AsIChatClient()
    };

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
        var clientOptions = new OpenAIClientOptions { NetworkTimeout = TimeSpan.FromSeconds(_options.TimeoutSeconds) };
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
