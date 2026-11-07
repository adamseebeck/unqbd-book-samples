using AdventureWorks.Core;
using Microsoft.Extensions.Options;

namespace AdventureWorks;

public static class DevelopmentEndpoints
{
    public static IEndpointRouteBuilder MapDevelopmentEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config",
            (IOptions<FeatureFlags> flags) =>
                Results.Ok(flags.Value));
        app.MapGet("/api/email-check", (string address,
            IEmailValidator validator) => Results.Ok(
                new { IsValid = validator.IsValid(address) }));
        MapRouteExamples(
            app.MapGroup("/examples").WithTags("Examples"));
        return app;
    }

    static void MapRouteExamples(RouteGroupBuilder group)
    {
        group.MapGet("/orders/{pageNumber:int?}",
            (int? pageNumber) => Results.Ok(
                new { Page = pageNumber ?? 1 }));
        group.MapGet("/status/{status:alpha=active}",
            (string status) => Results.Ok(
                new { Status = status }));
        group.MapGet("/files/{**path}",
            (string? path) => Results.Ok(new { Path = path }));
        group.MapGet("/unconstrained/{id}",
            (int id) => Results.Ok(new { Id = id }));
        group.MapGet("/reviews/{id:int:min(1)}/{reviewId:guid}",
            (int id, Guid reviewId) => Results.Ok(
                new { Id = id, ReviewId = reviewId }));
    }
}
