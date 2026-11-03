using System.ComponentModel.DataAnnotations;

namespace AdventureWorks;

public sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var model =
            context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
            return Results.BadRequest(
                "Request body is required.");
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(model,
            new ValidationContext(model), results,
            validateAllProperties: true);
        if (valid) return await next(context);
        var errors = results
            .SelectMany(result => result.MemberNames
                .DefaultIfEmpty("General")
                .Select(member => (member, result.ErrorMessage)))
            .GroupBy(item => item.member, item =>
                item.ErrorMessage ?? "Invalid value.")
            .ToDictionary(group => group.Key,
                group => group.ToArray());
        return Results.ValidationProblem(errors);
    }
}
