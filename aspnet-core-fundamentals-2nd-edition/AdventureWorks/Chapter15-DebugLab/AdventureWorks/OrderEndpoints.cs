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
        // Chapter 13: generated JSON code for the order API
        // first, reflection for everything else.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain
                .Insert(0, StoreJsonContext.Default));
        return services;
    }

    // The orders collection of the signed-in customer. Each order
    // is a resource: GET reads it, PATCH changes it, DELETE
    // cancels it. Finance archives; the Admin deletes.
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");
        orders.MapGet("/", ListOrders).WithName("ListOrders");
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
        orders.MapPost("/", CreateOrder);
        orders.MapPatch("/{id:int}", PatchOrder);
        orders.MapDelete("/{id:int}", CancelOrder);
        orders.MapPost("/{id:int}/archive", ArchiveOrder)
            .RequireAuthorization(
                StorePolicies.FinanceDepartment);
        orders.MapPost("/validate", (OrderApiInput input) =>
            TypedResults.Ok(new { input.Sku, input.Quantity }));
        app.MapDelete("/api/admin/orders/{id:int}", DeleteOrder)
            .WithTags("Orders")
            .RequireAuthorization(StorePolicies.AdminOnly);
        return app;
    }

    // One bounded page, with links to its neighbors in the Link
    // header (RFC 8288) and the total in the body.
    static async Task<IResult> ListOrders(IOrderService service,
        ClaimsPrincipal user, HttpContext context,
        LinkGenerator links,
        [Range(1, 1000)] int page = 1,
        [Range(1, 50)] int pageSize = 10)
    {
        var owner = user.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
        var result =
            await service.ListAsync(owner, page, pageSize);
        var body = new PagedResponse<OrderSummary>(result.Items,
            page, pageSize, result.TotalCount);
        List<string> rels = [];
        if (page > 1) rels.Add(Link(page - 1, "prev"));
        if (page < body.TotalPages)
            rels.Add(Link(page + 1, "next"));
        if (rels.Count > 0)
            context.Response.Headers.Link =
                string.Join(", ", rels);
        return Results.Ok(body);

        string Link(int target, string rel)
        {
            var uri = links.GetUriByName(context, "ListOrders",
                new { page = target, pageSize });
            return $"<{uri}>; rel=\"{rel}\"";
        }
    }

    static async Task<IResult> GetOrder(int id,
        IOrderRepository orders, IOptions<FeatureFlags> flags,
        IAuthorizationService authorization, ClaimsPrincipal user,
        HttpContext context)
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
        if (!access.Succeeded) return Results.Forbid();
        return Represent(context, order);
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
        if (result.Outcome != OrderOutcome.Created)
            return ToProblem(result);
        SetETag(context, result.Order!);
        return Results.CreatedAtRoute("GetOrderById",
            new { id = result.Order!.Id },
            OrderDto.From(result.Order));
    }

    // A conditional update: If-Match must carry the ETag the
    // client read. None is 428; a stale one is 412.
    static async Task<IResult> PatchOrder(int id,
        OrderNotesPatch patch, IOrderService service,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery, CancellationToken ct)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var ifMatch = context.Request.Headers.IfMatch.ToString();
        if (ifMatch.Length == 0)
            return Results.Problem("Send If-Match with the ETag.",
                statusCode:
                    StatusCodes.Status428PreconditionRequired);
        if (!Guid.TryParse(ifMatch.Trim('"'), out var version))
            return Results.Problem("If-Match is not an ETag.",
                statusCode:
                    StatusCodes.Status412PreconditionFailed);
        var owner = user.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
        var result = await service.UpdateNotesAsync(
            owner, id, patch.Notes, version, ct);
        return result.Outcome switch
        {
            OrderOutcome.Updated =>
                Represent(context, result.Order!),
            OrderOutcome.Conflict => Results.Problem(result.Error,
                statusCode:
                    StatusCodes.Status412PreconditionFailed),
            _ => ToProblem(result)
        };
    }

    // DELETE cancels: the order stays, with status Cancelled.
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
            user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            id, ct);
        return result.Outcome switch
        {
            OrderOutcome.Cancelled => Results.NoContent(),
            OrderOutcome.Invalid => Results.Problem(result.Error,
                statusCode: StatusCodes.Status409Conflict),
            _ => ToProblem(result)
        };
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

    static IResult Represent(HttpContext context, Order order)
    {
        SetETag(context, order);
        return Results.Ok(OrderDto.From(order));
    }

    // The Version concurrency token from Chapter 10, as an ETag.
    static void SetETag(HttpContext context, Order order) =>
        context.Response.Headers.ETag = $"\"{order.Version}\"";

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
