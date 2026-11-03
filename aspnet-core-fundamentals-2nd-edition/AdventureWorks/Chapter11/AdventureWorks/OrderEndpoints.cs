using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Data;
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
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddTransient<IEmailValidator, EmailValidator>();
        // Repositories and the unit of work share the request's
        // DbContext, so all three are scoped like it.
        services.AddScoped<IOrderWriter, EfOrderWriter>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        // The service, wrapped by its logging decorator.
        services.AddScoped<OrderService>();
        services.AddScoped<IOrderService>(provider =>
            new LoggingOrderService(
                provider.GetRequiredService<OrderService>(),
                provider.GetRequiredService<
                    ILogger<LoggingOrderService>>()));
        return services;
    }

    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
        orders.MapPost("/", CreateOrder);
        orders.MapPost("/{id:int}/cancel", CancelOrder);
        orders.MapGet("/report", GetReport);
        orders.MapPut("/{id:int}", UpdateNotes);
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
        var order = await orders.GetByIdAsync(id) ??
            throw new ResourceNotFoundException(
                $"Order {id} was not found.");
        // Load first, then authorize against the loaded order.
        var access = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderAccess);
        return access.Succeeded
            ? Results.Ok(order) : Results.Forbid();
    }

    // The endpoint adapts HTTP to the service: the owner comes
    // from the sign-in, and the outcome becomes a status code.
    static async Task<IResult> CreateOrder(
        CreateOrderRequest request, IOrderService service,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery, CancellationToken ct)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var owner = user.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
        var result =
            await service.CreateAsync(owner, request, ct);
        return result.Outcome == OrderOutcome.Created
            ? Results.CreatedAtRoute("GetOrderById",
                new { id = result.Order!.Id }, result.Order)
            : ToProblem(result);
    }

    // Chapter 9's owner rule first, then the service.
    static async Task<IResult> CancelOrder(int id,
        IOrderRepository orders, IOrderService service,
        IAuthorizationService authorization,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery, CancellationToken ct)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var order = await orders.GetByIdAsync(id);
        if (order is null) return Results.NotFound();
        var owner = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderOwner);
        if (!owner.Succeeded) return Results.Forbid();
        var result = await service.CancelAsync(
            order.OwnerId, id, ct);
        return result.Outcome == OrderOutcome.Cancelled
            ? Results.Ok(result.Order) : ToProblem(result);
    }

    static IResult ToProblem(OrderResult result) =>
        result.Outcome switch
        {
            OrderOutcome.NotFound => Results.NotFound(),
            OrderOutcome.Conflict => Results.Problem(result.Error,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(result.Error,
                statusCode: StatusCodes.Status400BadRequest)
        };

    // The signed-in owner's orders, one bounded page at a time.
    static async Task<IResult> GetReport(
        IOrderRepository orders, ClaimsPrincipal user,
        [Range(1, 1000)] int page = 1)
    {
        var owner = user.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
        return Results.Ok(new
        {
            Page = page,
            Orders = await orders.ListAsync(owner, page)
        });
    }

    // The client sends back the Version it read. If the order has
    // changed since, the update changes nothing: 409.
    static async Task<IResult> UpdateNotes(int id,
        OrderNotesInput input, IOrderRepository orders,
        IAuthorizationService authorization,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var order = await orders.GetByIdAsync(id);
        if (order is null) return Results.NotFound();
        var owner = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderOwner);
        if (!owner.Succeeded) return Results.Forbid();
        var outcome = await orders.UpdateNotesAsync(
            id, input.Notes, input.Version);
        return outcome switch
        {
            // Null: deleted after the update, before this read.
            UpdateOutcome.Updated =>
                await orders.GetByIdAsync(id) is { } saved
                    ? Results.Ok(saved) : Results.NotFound(),
            UpdateOutcome.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The order has changed",
                detail: "Read the order again and send its "
                    + "current version."),
            _ => Results.NotFound()
        };
    }

    static async Task<IResult> DeleteOrder(int id,
        IOrderRepository orders, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        return await orders.DeleteAsync(id)
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
        var order = await orders.GetByIdAsync(id);
        if (order is null) return Results.NotFound();
        var owner = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderOwner);
        if (!owner.Succeeded) return Results.Forbid();
        return await orders.ArchiveAsync(id)
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
