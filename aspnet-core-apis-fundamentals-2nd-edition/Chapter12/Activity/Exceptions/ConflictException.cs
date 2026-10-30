namespace WarehouseApi.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string message, string conflictReason, Exception? innerException = null)
        : base(message, innerException)
    {
        ConflictReason = conflictReason;
    }

    // A short code clients can branch on, such as INSUFFICIENT_STOCK
    public string ConflictReason { get; }

    public override string ErrorType => "https://api.example.com/errors/conflict";
    public override string Title => "Conflict";
    public override int StatusCode => StatusCodes.Status409Conflict;
}
