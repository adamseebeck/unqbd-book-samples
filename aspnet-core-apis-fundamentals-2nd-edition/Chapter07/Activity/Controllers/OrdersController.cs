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

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
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

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<OrderResponse>>> Search(
        [FromQuery] OrderSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        // Creating an order is not idempotent, so a retry needs a key, as a stock adjustment does
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problem(detail: "The Idempotency-Key header is required.", statusCode: StatusCodes.Status400BadRequest, title: "Missing Idempotency Key");
        }

        var createdOrder = await _orderService.CreateAsync(request, idempotencyKey, cancellationToken);

        if (createdOrder == null)
        {
            return Problem(detail: "Idempotency key reused with a different request payload.", statusCode: StatusCodes.Status422UnprocessableEntity, title: "Idempotency Key Reused");
        }

        // The first request and every retry get 201 with the same order and Location
        return CreatedAtAction(
            nameof(GetById),
            new { id = createdOrder.Id },
            createdOrder);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        // [ApiController] has already rejected a request without NewStatus
        var newStatus = request.NewStatus!.Value;

        try
        {
            var updatedOrder = await _orderService.UpdateStatusAsync(id, newStatus, cancellationToken);

            if (updatedOrder == null)
            {
                return NotFound();
            }

            return Ok(updatedOrder);
        }
        catch (InvalidOperationException ex)
        {
            // A stopgap: .NET throws this type for unrelated bugs too, which would reach the client as 409;
            // Chapter 12 replaces it with exception types that only the domain code throws
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid State Transition");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _orderService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
