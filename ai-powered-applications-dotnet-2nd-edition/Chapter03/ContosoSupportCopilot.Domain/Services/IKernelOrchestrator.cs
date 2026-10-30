namespace ContosoSupportCopilot.Domain.Services;

public interface IKernelOrchestrator
{
    Task<string> ExecutePromptAsync(string promptTemplate, Dictionary<string, object> variables, CancellationToken cancellationToken = default);
    Task<T> ExecuteStructuredPromptAsync<T>(string promptTemplate, Dictionary<string, object> variables, CancellationToken cancellationToken = default) where T : class;
    void RegisterPlugin(string pluginName, object pluginFunctions);
    Task<string> ExecutePluginFunctionAsync(string pluginName, string functionName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}
