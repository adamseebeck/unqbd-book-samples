namespace WarehouseApi.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} with identifier '{resourceId}' was not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public string ResourceType { get; }
    public string ResourceId { get; }

    // A missing resource means nothing beyond its status code, so it keeps the standard type and title
    // that ASP.NET Core gives every other 404: GetById and a cancellation answer a missing order alike
    public override string ErrorType => "https://tools.ietf.org/html/rfc9110#section-15.5.5";
    public override string Title => "Not Found";
    public override int StatusCode => StatusCodes.Status404NotFound;
}
