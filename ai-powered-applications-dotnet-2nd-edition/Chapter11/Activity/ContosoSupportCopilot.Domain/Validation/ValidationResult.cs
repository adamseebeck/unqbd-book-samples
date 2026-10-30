using System.Diagnostics.CodeAnalysis;

namespace ContosoSupportCopilot.Domain.Validation;

public sealed record ValidationResult<T> where T : class
{
    private ValidationResult(bool isSuccess, T? value, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }

    public static ValidationResult<T> Success(T value) => new(true, value, []);

    public static ValidationResult<T> Failure(IEnumerable<string> errors) => new(false, null, errors.ToList());
}
