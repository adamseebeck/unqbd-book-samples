namespace ContosoSupportCopilot.Domain.Validation;

public sealed class SchemaValidationException : Exception
{
    public SchemaValidationException(
        string message,
        string rawOutput,
        IReadOnlyList<string> validationErrors)
        : base(message)
    {
        RawOutput = rawOutput;
        ValidationErrors = validationErrors;
    }

    public string RawOutput { get; }
    public IReadOnlyList<string> ValidationErrors { get; }
}
