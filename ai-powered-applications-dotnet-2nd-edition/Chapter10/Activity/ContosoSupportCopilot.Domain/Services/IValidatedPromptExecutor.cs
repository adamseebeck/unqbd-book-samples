using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Validation;

namespace ContosoSupportCopilot.Domain.Services;

public interface IValidatedPromptExecutor
{
    Task<T> ExecuteAsync<T>(
        PromptTemplate template,
        IReadOnlyDictionary<string, object> variables,
        IOutputValidator<T> validator,
        CancellationToken cancellationToken = default) where T : class;
}
