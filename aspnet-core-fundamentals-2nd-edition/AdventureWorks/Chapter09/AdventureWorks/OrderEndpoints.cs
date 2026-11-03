using System.Security.Claims;
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
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
        services.AddSingleton<IOrderRepository,
            OrderRepository>();
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
        orders.MapDelete("/{id:int}", DeleteOrder)
            .RequireAuthorization(StorePolicies.AdminOnly);
        orders.MapPost("/{id:int}/archive", ArchiveOrder)
            .RequireAuthorization(
                StorePolicies.FinanceDepartment);
        orders.MapPost("/validate", (OrderApiInput input) =>
            TypedResults.Ok(new { input.Sku, input.Quantity }));
        return app;
    }

    static async Task<IResult> GetOrder(int id,
        IOrderRepository orders, IOptions<FeatureFlags> flags,
        IAuthorizationService authorization, ClaimsPrincipal user)
    {
        if (!flags.Value.EnableNewCheckout)
            return Results.StatusCode(
                StatusCodes.Status503ServiceUnavailable);
        if (id <= 0)
            throw new DomainValidationException(
                "Order ID must be positive.");
        var order = orders.GetById(id) ??
            throw new ResourceNotFoundException(
                $"Order {id} was not found.");
        // Load first, then authorize against the loaded order.
        var access = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderAccess);
        return access.Succeeded
            ? Results.Ok(order) : Results.Forbid();
    }

    static async Task<IResult> DeleteOrder(int id,
        IOrderRepository orders, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        return orders.Delete(id)
            ? Results.NoContent() : Results.NotFound();
    }

    // The endpoint policy already required Department:Finance.
    // The owner rule needs the loaded order, so it runs here.
    static async Task<IResult> ArchiveOrder(int id,
        IOrderRepository orders,
        IAuthorizationService authorization,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var order = orders.GetById(id);
        if (order is null) return Results.NotFound();
        var owner = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderOwner);
        if (!owner.Succeeded) return Results.Forbid();
        return orders.Archive(id)
            ? Results.NoContent() : Results.NotFound();
    }

    static async Task<bool> HasValidTokenAsync(
        HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}
