using System.Diagnostics;
using System.Text.Json;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Validation;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Polly.Timeout;

namespace ContosoSupportCopilot.Infrastructure.AI;

public sealed class ValidatedPromptExecutor : IValidatedPromptExecutor
{
    private readonly IChatClient _chatClient;
    private readonly ResiliencePipeline _pipeline;
    private readonly bool _strictStructuredOutput;
    private readonly ReasoningEffort? _reasoningEffort;
    private readonly TokenPricingOptions _pricing;
    private readonly ILogger<ValidatedPromptExecutor> _logger;

    public ValidatedPromptExecutor(
        IChatClient chatClient,
        ResiliencePipelineProvider<string> pipelines,
        IOptions<AIProviderOptions> options,
        IOptions<TokenPricingOptions> pricing,
        ILogger<ValidatedPromptExecutor> logger)
    {
        _chatClient = chatClient;
        _pipeline = pipelines.GetPipeline(StructuredOutputPipeline.Name);
        _strictStructuredOutput = options.Value.StrictStructuredOutput;
        _reasoningEffort = options.Value.StructuredOutputReasoningEffort;
        _pricing = pricing.Value;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        PromptTemplate template,
        IReadOnlyDictionary<string, object> variables,
        IOutputValidator<T> validator,
        CancellationToken cancellationToken = default) where T : class
    {
        var prompt = template.Render(variables);
        var options = new ChatOptions
        {
            Instructions = template.Instructions,
            MaxOutputTokens = 2000
        };

        if (_reasoningEffort is { } effort)
        {
            // Only for reasoning deployments: a lower effort answers simple classifications sooner.
            options.Reasoning = new ReasoningOptions { Effort = effort };
        }

        if (_strictStructuredOutput)
        {
            // OpenAI and Foundry models then follow the JSON schema exactly (strict structured output).
            options.AdditionalProperties = new() { ["strict"] = true };
        }

        try
        {
            return await _pipeline.ExecuteAsync(
                token => ExecuteOnceAsync(template, prompt, options, validator, token),
                cancellationToken);
        }
        catch (TimeoutRejectedException ex)
        {
            // Polly's exceptions stay in the Infrastructure project, as in the Chapter 4 chat client.
            throw new TimeoutException($"{template.Name} produced no valid answer within the total timeout.", ex);
        }
    }

    private async ValueTask<T> ExecuteOnceAsync<T>(
        PromptTemplate template,
        string prompt,
        ChatOptions options,
        IOutputValidator<T> validator,
        CancellationToken cancellationToken) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        ChatResponse<T> response = await _chatClient.GetResponseAsync<T>(
            prompt, StructuredOutputJson.Options, options, cancellationToken: cancellationToken);

        var usage = response.Usage;
        _logger.LogInformation(
            "{Template} v{Version} answered in {LatencyMs}ms. Tokens: {Input} input ({Cached} cached), {Output} output ({Reasoning} reasoning). Cost: ${Cost:F6}",
            template.Name,
            template.Version,
            stopwatch.ElapsedMilliseconds,
            usage?.InputTokenCount,
            usage?.CachedInputTokenCount,
            usage?.OutputTokenCount,
            usage?.ReasoningTokenCount,
            TokenCostCalculator.Calculate(usage, _pricing));

        return Validate(template, response, validator);
    }

    private T Validate<T>(PromptTemplate template, ChatResponse<T> response, IOutputValidator<T> validator) where T : class
    {
        if (response.FinishReason == ChatFinishReason.Length)
        {
            throw new InvalidOperationException(
                $"The answer to {template.Name} reached MaxOutputTokens before the JSON was complete.");
        }

        IReadOnlyList<string> errors;
        try
        {
            // Strict deserialization checks the shape; the validator checks the values.
            var result = validator.Validate(response.Result);
            if (result.IsSuccess)
            {
                return result.Value;
            }

            errors = result.Errors;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            errors = [ex.Message];
        }

        _logger.LogWarning(
            "{Template} v{Version} returned invalid output: {Errors}",
            template.Name,
            template.Version,
            string.Join("; ", errors));
        _logger.LogDebug("Rejected output for {Template}: {RawOutput}", template.Name, response.Text);

        throw new SchemaValidationException($"The answer to {template.Name} failed validation.", response.Text, errors);
    }
}
