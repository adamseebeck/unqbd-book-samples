namespace ContosoSupportCopilot.Domain.Services;

public interface IKernelOrchestrator
{
    Task<string> ExecutePromptAsync(string promptTemplate, Dictionary<string, object> variables, CancellationToken cancellationToken = default);
    // From Chapter 5, structured output goes through IValidatedPromptExecutor instead.
    [Obsolete("Use IValidatedPromptExecutor, which requests a schema, validates the answer, and retries invalid output.")]
    Task<T> ExecuteStructuredPromptAsync<T>(string promptTemplate, Dictionary<string, object> variables, CancellationToken cancellationToken = default) where T : class;
    void RegisterPlugin(string pluginName, object pluginFunctions);
    Task<string> ExecutePluginFunctionAsync(string pluginName, string functionName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}
