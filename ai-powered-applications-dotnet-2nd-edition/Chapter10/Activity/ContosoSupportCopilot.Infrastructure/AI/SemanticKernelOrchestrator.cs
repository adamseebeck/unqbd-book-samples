using System.Text.Json;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly.CircuitBreaker;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class SemanticKernelOrchestrator : IKernelOrchestrator
{
    private static readonly JsonSerializerOptions StructuredJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelOrchestrator> _logger;

    // The kernel's chat client runs every model call through the resilience pipeline. It reports
    // a timeout as TimeoutException and an open circuit as InvalidOperationException.
    public SemanticKernelOrchestrator(
        Kernel kernel,
        ILogger<SemanticKernelOrchestrator> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> ExecutePromptAsync(
        string promptTemplate,
        Dictionary<string, object> variables,
        CancellationToken cancellationToken = default)
    {
        var function = _kernel.CreateFunctionFromPrompt(promptTemplate);

        try
        {
            var result = await _kernel.InvokeAsync(function, ToArguments(variables), cancellationToken);
            return result.ToString();
        }
        catch (InvalidOperationException ex) when (ex.InnerException is BrokenCircuitException)
        {
            _logger.LogWarning(ex, "The AI service circuit is open; returning the fallback message");
            return "[AI service temporarily unavailable. Please try again later.]";
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "The prompt timed out; returning the fallback message");
            return "[Request timed out. The AI service is experiencing high latency.]";
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "The prompt failed; returning the fallback message");
            return "[An error occurred while processing your request.]";
        }
    }

    public async Task<T> ExecuteStructuredPromptAsync<T>(
        string promptTemplate,
        Dictionary<string, object> variables,
        CancellationToken cancellationToken = default) where T : class
    {
        // Ask for JSON mode, so the model returns a JSON object without surrounding text.
        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object> { ["response_format"] = "json_object" }
        };
        var function = _kernel.CreateFunctionFromPrompt(promptTemplate, settings);

        string content;
        try
        {
            var result = await _kernel.InvokeAsync(function, ToArguments(variables), cancellationToken);
            content = result.ToString();
        }
        catch (Exception ex) when (ex is not TimeoutException
            && ex.InnerException is not BrokenCircuitException
            && (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
        {
            // Timeouts, an open circuit, and the caller's cancellation pass through unchanged.
            throw new InvalidOperationException("Unexpected error during structured prompt execution.", ex);
        }

        try
        {
            // Missing constructor values and nulls for non-nullable members fail here, not later.
            return JsonSerializer.Deserialize<T>(content, StructuredJsonOptions)
                ?? throw new InvalidOperationException($"The model returned null instead of a {typeof(T).Name}.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "The model's answer is not a valid {Type}", typeof(T).Name);
            throw new InvalidOperationException($"The model's answer is not a valid {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    public void RegisterPlugin(string pluginName, object pluginFunctions)
    {
        _kernel.ImportPluginFromObject(pluginFunctions, pluginName);
    }

    public async Task<string> ExecutePluginFunctionAsync(
        string pluginName,
        string functionName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        // Native functions run in this process, so they do not go through the AI service pipeline.
        var result = await _kernel.InvokeAsync(pluginName, functionName, ToArguments(parameters), cancellationToken);

        return result.GetValue<object>() switch
        {
            null => string.Empty,
            string text => text,
            var value => JsonSerializer.Serialize(value)
        };
    }

    private static KernelArguments ToArguments(Dictionary<string, object> variables)
    {
        var arguments = new KernelArguments();
        foreach (var (name, value) in variables)
        {
            arguments[name] = value;
        }

        return arguments;
    }
}
