using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using Microsoft.Extensions.Options;

namespace AdventureWorks;

public static class OrderEndpoints
{
    public static IServiceCollection AddOrderServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FeatureFlags>()
            .Bind(configuration.GetSection("FeatureFlags"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ICacheService,
            MemoryCacheService>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddTransient<IEmailValidator, EmailValidator>();
        return services;
    }

    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
        orders.MapPost("/validate", (OrderApiInput input) =>
            TypedResults.Ok(new { input.Sku, input.Quantity }));
        return app;
    }

    static IResult GetOrder(int id, IOrderRepository orders,
        IOptions<FeatureFlags> flags)
    {
        if (!flags.Value.EnableNewCheckout)
            return Results.StatusCode(
                StatusCodes.Status503ServiceUnavailable);
        if (id <= 0)
            throw new DomainValidationException(
                "Order ID must be positive.");
        return Results.Ok(orders.GetById(id) ??
            throw new ResourceNotFoundException(
                $"Order {id} was not found."));
    }
}
