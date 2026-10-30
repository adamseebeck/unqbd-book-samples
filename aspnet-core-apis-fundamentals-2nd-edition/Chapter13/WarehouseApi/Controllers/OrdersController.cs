using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Exceptions;
using WarehouseApi.Security;
using WarehouseApi.Services;

namespace WarehouseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IReservationService _reservationService;
    private readonly IAuthorizationService _authorizationService;

    public OrdersController(
        IOrderService orderService,
        IReservationService reservationService,
        IAuthorizationService authorizationService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetList(
        [FromQuery] OrderStatus? status,
        [FromQuery, Range(1, 100_000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Each caller sees the orders of its own scope: OrderReadScope holds the rule
        var orders = await _orderService.GetOrdersAsync(status, page, pageSize, OrderReadScope.For(User), cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, OrderReadScope.For(User), cancellationToken);

        // Looking up a missing order is an expected outcome, not an exception; so is an order the caller may not read
        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    // The actions below contain no try/catch: a domain exception reaches the exception handler

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireIntegrationClientOrSupervisor)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        // Creating an order is not idempotent, so a retry needs a key, as a stock adjustment does
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problem(detail: "The Idempotency-Key header is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        // The API accepts keys up to this length, the length of the idempotency table's key column
        if (idempotencyKey.Length > IdempotencyKeyRules.MaxLength)
        {
            return Problem(detail: $"Idempotency-Key must be at most {IdempotencyKeyRules.MaxLength} characters.", statusCode: StatusCodes.Status400BadRequest);
        }

        // The order remembers its creator, which decides who may read it and whose retries replay it
        var createdOrder = await _orderService.CreateAsync(
            request, idempotencyKey, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);

        // An expected outcome, not an exception: the key belongs to a different request or another caller
        if (createdOrder == null)
        {
            return Problem(detail: "Idempotency key reused with a different request payload.", statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        // The first request and every retry get 201 with the same order and Location
        return CreatedAtAction(
            nameof(GetById),
            new { id = createdOrder.Id },
            createdOrder);
    }

    [HttpPost("{id:guid}/allocate")]
    [Authorize(Policy = AuthorizationPolicies.RequireSupervisor)]
    public async Task<ActionResult<OrderResponse>> Allocate(
        Guid id,
        [FromBody] AllocateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _reservationService.ReserveAsync(new ReservationRequest(id, request.WarehouseCode), cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/fulfill")]
    public async Task<ActionResult<OrderResponse>> Fulfill(Guid id, CancellationToken cancellationToken)
    {
        // An order outside the caller's read scope is not found, as in GetById, so the answer never confirms
        // that another partner's order exists. The warehouse is part of the stored order, not of the URL,
        // so the action then checks the fulfillment permission itself
        var existing = await _orderService.GetByIdAsync(id, OrderReadScope.For(User), cancellationToken)
            ?? throw new OrderNotFoundException(id);

        var access = await _authorizationService.AuthorizeAsync(
            User, existing.FulfillmentWarehouseCode, new WarehouseAccessRequirement(Permissions.Fulfill));

        if (!access.Succeeded)
        {
            return Forbid();
        }

        var order = await _orderService.FulfillAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.RequireSupervisor)]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.CancelAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireSupervisor)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _orderService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
