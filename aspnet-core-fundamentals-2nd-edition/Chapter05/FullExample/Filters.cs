using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var model = context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null) return Results.BadRequest("Request body is required.");
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, true))
        {
            var errors = results.SelectMany(result =>
                result.MemberNames.DefaultIfEmpty("General").Select(member =>
                    new { Member = member, Error = result.ErrorMessage ?? "Invalid value." }))
                .GroupBy(item => item.Member)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Error).ToArray());
            return Results.ValidationProblem(errors);
        }
        return await next(context);
    }
}

public sealed class RequestLoggingFilter(ILogger<RequestLoggingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        logger.LogDebug("Starting action {Action}", context.ActionDescriptor.DisplayName);
        await next();
    }
}

public sealed class RequestLoggingEndpointFilter(ILogger<RequestLoggingEndpointFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        logger.LogDebug("Starting endpoint {Endpoint}", context.HttpContext.GetEndpoint()?.DisplayName);
        return await next(context);
    }
}
