namespace AdventureWorks.Core;

public sealed class DomainValidationException(string message)
    : Exception(message);

public sealed class ResourceNotFoundException(string message)
    : Exception(message);
