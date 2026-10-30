using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IReservationService _reservationService;

    public OrdersController(IOrderService orderService, IReservationService reservationService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetList(
        [FromQuery] OrderStatus? status,
        [FromQuery, Range(1, 100_000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetOrdersAsync(status, page, pageSize, cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);

        // Looking up a missing order is an expected outcome, not an exception
        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    // The actions below contain no try/catch: a domain exception reaches the exception handler

    [HttpPost]
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

        var createdOrder = await _orderService.CreateAsync(request, idempotencyKey, cancellationToken);

        // An expected outcome, not an exception: the key belongs to a different request
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
        var order = await _orderService.FulfillAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.CancelAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _orderService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
