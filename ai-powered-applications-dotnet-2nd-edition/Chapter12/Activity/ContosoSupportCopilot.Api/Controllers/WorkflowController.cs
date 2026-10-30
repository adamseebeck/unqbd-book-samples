using System.Globalization;
using ContosoSupportCopilot.Domain.Workflows;
using ContosoSupportCopilot.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace ContosoSupportCopilot.Api.Controllers;

public record ProcessTicketRequest(string InitialInput);

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly ISupportWorkflow _workflow;
    private readonly EngineerIdentity _identity;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(ISupportWorkflow workflow, EngineerIdentity identity, ILogger<WorkflowController> logger)
    {
        _workflow = workflow;
        _identity = identity;
        _logger = logger;
    }

    // From Chapter 12: the engineer is the signed-in user, or in Development the X-Engineer-Id header.
    // Without either, the JwtBearer handler answers 401 with a WWW-Authenticate header; a valid token
    // without the Copilot.Use scope, an app-only token for example, gets 403.
    [HttpPost("tickets/{ticketId:guid}/process")]
    public async Task<ActionResult<WorkflowResult>> ProcessTicket(
        Guid ticketId,
        [FromBody] ProcessTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (_identity.GetEngineerId(HttpContext) is not { } engineerId)
        {
            return Refuse();
        }

        try
        {
            return ToActionResult(await _workflow.ExecuteAsync(ticketId, engineerId, request.InitialInput, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WorkflowConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // Only the engineer who started a workflow can resume it; for anyone else it does not exist.
    [HttpPost("{workflowId:guid}/resume")]
    public async Task<ActionResult<WorkflowResult>> Resume(Guid workflowId, CancellationToken cancellationToken)
    {
        if (_identity.GetEngineerId(HttpContext) is not { } engineerId)
        {
            return Refuse();
        }

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

    private ActionResult Refuse() => User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

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
