using System.Diagnostics;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AdventureWorks;

public sealed class QuoteTimingFilter(
    ILogger<QuoteTimingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            return await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            logger.LogInformation("Quote endpoint took {Ms} ms",
                elapsed.TotalMilliseconds);
        }
    }
}

public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(
        this IEndpointRouteBuilder app)
    {
        var quotes = app.MapGroup("/api/quotes")
            .WithTags("Quotes")
            .AddEndpointFilter<QuoteTimingFilter>();
        quotes.MapGet("/{id:int}", GetQuote).WithName("GetQuote");
        quotes.MapPost("/", CreateQuote)
            .AddEndpointFilter<
                ValidationFilter<CreateQuoteRequest>>()
            .ProducesValidationProblem();
        return app;
    }

    static Results<Ok<QuoteDto>, NotFound> GetQuote(
        int id, QuoteStore store) =>
        store.Find(id) is { } quote
            ? TypedResults.Ok(quote) : TypedResults.NotFound();

    static async Task<Results<CreatedAtRoute<QuoteDto>,
        ValidationProblem>> CreateQuote(
        CreateQuoteRequest request,
        IProductService products, QuoteStore store)
    {
        var product =
            await products.GetByIdAsync(request.ProductId);
        if (product is null)
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["ProductId"] = ["Unknown product."]
                });
        var total = product.Price * request.Quantity;
        var quote = store.Add(
            product.Id, request.Quantity, total);
        return TypedResults.CreatedAtRoute(
            quote, "GetQuote", new { id = quote.Id });
    }
}
