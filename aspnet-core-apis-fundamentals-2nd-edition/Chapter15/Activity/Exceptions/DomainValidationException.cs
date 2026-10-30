namespace WarehouseApi.Exceptions;

// Named to avoid a clash with System.ComponentModel.DataAnnotations.ValidationException
public class DomainValidationException : DomainException
{
    public DomainValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    // Keyed like the validation errors from Chapter 8, for example Lines[0].Sku
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public override string ErrorType => "https://api.example.com/errors/validation";
    public override string Title => "Validation Failed";
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
}
