using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.Submissions.Commands;
using Vector.Application.Submissions.DTOs;
using Vector.Application.Submissions.Queries;

namespace Vector.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SubmissionsController(IMediator mediator, ILogger<SubmissionsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets a submission by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var submission = await mediator.Send(new GetSubmissionQuery(id), cancellationToken);

        if (submission is null)
        {
            return NotFound();
        }

        return Ok(submission);
    }

    /// <summary>
    /// Creates a new submission.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSubmissionCommand(
            request.TenantId,
            request.InsuredName,
            request.ProcessingJobId,
            request.InboundEmailId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to create submission",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Created submission {SubmissionId}", result.Value);

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Assigns a submission to an underwriter.
    /// </summary>
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignSubmissionRequest request, CancellationToken cancellationToken)
    {
        var command = new AssignSubmissionCommand(id, request.UnderwriterId, request.UnderwriterName);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to assign submission",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Assigned submission {SubmissionId} to {UnderwriterName}", id, request.UnderwriterName);

        return NoContent();
    }
}

public record CreateSubmissionRequest(
    Guid TenantId,
    string InsuredName,
    Guid? ProcessingJobId = null,
    Guid? InboundEmailId = null);

public record AssignSubmissionRequest(
    Guid UnderwriterId,
    string UnderwriterName);
