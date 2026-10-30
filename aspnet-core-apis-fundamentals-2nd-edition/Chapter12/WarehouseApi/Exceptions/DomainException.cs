namespace WarehouseApi.Exceptions;

// A business rule failure; each type declares the problem type, its title, and the status code it maps to
public abstract class DomainException : Exception
{
    protected DomainException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public abstract string ErrorType { get; }

    // A short summary of the problem type for people (RFC 9457 title)
    public abstract string Title { get; }
    public abstract int StatusCode { get; }
}
