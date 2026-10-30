using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Configuration;
using WarehouseApi.Contracts;
using WarehouseApi.Contracts.V2;
using WarehouseApi.Security;
using WarehouseApi.Services;

namespace WarehouseApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/stock-adjustments")]
[Authorize(Policy = AuthorizationPolicies.RequireSupervisor)]
[ApiVersion(ApiVersionConstants.V1, Deprecated = true)]
[ApiVersion(ApiVersionConstants.V2)]
[Produces("application/json", "application/xml")]
public class StockAdjustmentsController : ControllerBase
{
    private readonly IStockHistoryService _historyService;

    public StockAdjustmentsController(IStockHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet]
    [MapToApiVersion(ApiVersionConstants.V1)]
    [ProducesResponseType<IEnumerable<StockAdjustmentResponseV1>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StockAdjustmentResponseV1>>> GetRecentV1(
        [FromQuery, Required] string warehouseCode,
        [FromQuery, Range(1, 50)] int count = 20,
        CancellationToken cancellationToken = default)
    {
        var adjustments = await _historyService.GetRecentAsync(warehouseCode, count, cancellationToken);

        // ToList: the XML formatter cannot write a LINQ query that has not run yet
        return Ok(adjustments.Select(ToV1).ToList());
    }

    [HttpGet]
    [MapToApiVersion(ApiVersionConstants.V2)]
    [ProducesResponseType<IEnumerable<StockAdjustmentResponseV2>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StockAdjustmentResponseV2>>> GetRecentV2(
        [FromQuery, Required] string warehouseCode,
        [FromQuery, Range(1, 50)] int count = 20,
        CancellationToken cancellationToken = default)
    {
        var adjustments = await _historyService.GetRecentAsync(warehouseCode, count, cancellationToken);
        return Ok(adjustments.Select(ToV2).ToList());
    }

    [HttpGet("{id:guid}")]
    [MapToApiVersion(ApiVersionConstants.V1)]
    [ProducesResponseType<StockAdjustmentResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json", "application/problem+xml")]
    public async Task<ActionResult<StockAdjustmentResponseV1>> GetV1(Guid id, CancellationToken cancellationToken)
    {
        var adjustment = await _historyService.GetAdjustmentAsync(id, cancellationToken);
        return adjustment is null ? NotFound() : Ok(ToV1(adjustment));
    }

    [HttpGet("{id:guid}")]
    [MapToApiVersion(ApiVersionConstants.V2)]
    [ProducesResponseType<StockAdjustmentResponseV2>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json", "application/problem+xml")]
    public async Task<ActionResult<StockAdjustmentResponseV2>> GetV2(Guid id, CancellationToken cancellationToken)
    {
        var adjustment = await _historyService.GetAdjustmentAsync(id, cancellationToken);
        return adjustment is null ? NotFound() : Ok(ToV2(adjustment));
    }

    private static StockAdjustmentResponseV1 ToV1(StockAdjustmentDetails a) =>
        new(a.Id, a.ProductSku, a.WarehouseCode, a.QuantityChange, a.ReasonCode);

    private static StockAdjustmentResponseV2 ToV2(StockAdjustmentDetails a) =>
        new(a.Id, a.ProductSku, a.WarehouseCode, a.QuantityChange, a.ReasonCode,
            a.ReservedChange, a.ReferenceNumber, a.CorrelationId, a.AdjustedAt);
}
