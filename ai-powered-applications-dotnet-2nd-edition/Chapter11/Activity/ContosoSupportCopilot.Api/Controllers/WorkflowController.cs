using System.Globalization;
using ContosoSupportCopilot.Domain.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace ContosoSupportCopilot.Api.Controllers;

public record ProcessTicketRequest(string InitialInput);

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly ISupportWorkflow _workflow;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(ISupportWorkflow workflow, ILogger<WorkflowController> logger)
    {
        _workflow = workflow;
        _logger = logger;
    }

    // The sample has no sign-in, so the engineer ID arrives in a header. Behind authentication,
    // read it from the signed-in user's claims instead: anyone can send a header.
    [HttpPost("tickets/{ticketId:guid}/process")]
    public async Task<ActionResult<WorkflowResult>> ProcessTicket(
        Guid ticketId,
        [FromHeader(Name = "X-Engineer-Id")] string engineerId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] ProcessTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (idempotencyKey is { Length: > 100 })
        {
            return BadRequest("The Idempotency-Key header can hold at most 100 characters.");
        }

        try
        {
            return ToActionResult(await _workflow.ExecuteAsync(ticketId, engineerId, request.InitialInput, idempotencyKey, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WorkflowConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (IdempotencyKeyReusedException ex)
        {
            // 422 with a Problem Details body: the request is well formed, but its key belongs to another request
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }

    // Only the engineer who started a workflow can resume it; for anyone else it does not exist.
    [HttpPost("{workflowId:guid}/resume")]
    public async Task<ActionResult<WorkflowResult>> Resume(
        Guid workflowId,
        [FromHeader(Name = "X-Engineer-Id")] string engineerId,
        CancellationToken cancellationToken)
    {
        try
        {
            return ToActionResult(await _workflow.ResumeAsync(workflowId, engineerId, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (WorkflowConflictException ex)
        {
            // Another request is running this workflow: it saved a newer state first.
            return Conflict(ex.Message);
        }
    }

    // Completed runs, including escalations, are 200. A run that a transient failure stopped is 503 with a
    // Retry-After header: resume it then. A failed run is 500 without its stored error, which can hold provider or
    // database details; the log keeps the error, and the response names the correlation ID to find it.
    private ActionResult<WorkflowResult> ToActionResult(WorkflowResult result)
    {
        if (result.RetryAfter is { } retryAfter)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling((retryAfter - DateTime.UtcNow).TotalSeconds));
            Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }

        if (result.FinalStep != WorkflowStep.Failed)
        {
            return Ok(result);
        }

        // The run's own correlation ID, which the response quotes; a resume request's log scope carries its own.
        _logger.LogError("Workflow {WorkflowId} ({CorrelationId}) failed: {Error}",
            result.WorkflowId, result.CorrelationId, result.ErrorMessage);
        return StatusCode(StatusCodes.Status500InternalServerError,
            result with { ErrorMessage = $"The workflow failed. Quote correlation ID {result.CorrelationId} when you report it." });
    }
}
