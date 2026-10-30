namespace ContosoSupportCopilot.Domain.Validation;

public interface IOutputValidator<T> where T : class
{
    ValidationResult<T> Validate(T value);
}
