using System.Text.Json;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.SemanticKernel;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class SemanticKernelOrchestrator : IKernelOrchestrator
{
    private readonly Kernel _kernel;

    public SemanticKernelOrchestrator(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> ExecutePromptAsync(string promptTemplate, Dictionary<string, object> variables, CancellationToken cancellationToken = default)
    {
        var function = _kernel.CreateFunctionFromPrompt(promptTemplate);
        var result = await _kernel.InvokeAsync(function, ToArguments(variables), cancellationToken);
        return result.ToString();
    }

    public async Task<T> ExecuteStructuredPromptAsync<T>(string promptTemplate, Dictionary<string, object> variables, CancellationToken cancellationToken = default) where T : class
    {
        var function = _kernel.CreateFunctionFromPrompt(promptTemplate);
        var result = await _kernel.InvokeAsync(function, ToArguments(variables), cancellationToken);

        var json = result.ToString();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to deserialize structured output");
    }

    public void RegisterPlugin(string pluginName, object pluginFunctions)
    {
        _kernel.ImportPluginFromObject(pluginFunctions, pluginName);
    }

    public async Task<string> ExecutePluginFunctionAsync(string pluginName, string functionName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var result = await _kernel.InvokeAsync(pluginName, functionName, ToArguments(parameters), cancellationToken);
        return result.ToString();
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
