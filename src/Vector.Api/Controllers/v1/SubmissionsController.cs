using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.Submissions.Commands;
using Vector.Application.Submissions.DTOs;
using Vector.Application.Submissions.Queries;
using Vector.Domain.Submission.Enums;

namespace Vector.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SubmissionsController(IMediator mediator, ILogger<SubmissionsController> logger) : ControllerBase
{
    /// <summary>
    /// Lists submissions for the current user with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProducerSubmissionsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? producerId,
        [FromQuery] SubmissionStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProducerSubmissionsQuery(
            producerId,
            status,
            search,
            page,
            pageSize);

        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

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
    /// Gets a submission by submission number (e.g., SUB-2024-000001).
    /// </summary>
    [HttpGet("by-number/{submissionNumber}")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string submissionNumber, CancellationToken cancellationToken)
    {
        var submission = await mediator.Send(new GetSubmissionByNumberQuery(submissionNumber), cancellationToken);

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

    /// <summary>
    /// Quotes a submission with a premium amount.
    /// </summary>
    [HttpPost("{id:guid}/quote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Quote(Guid id, [FromBody] QuoteSubmissionRequest request, CancellationToken cancellationToken)
    {
        var command = new QuoteSubmissionCommand(id, request.PremiumAmount, request.Currency);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to quote submission",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Quoted submission {SubmissionId} with premium {Amount} {Currency}", id, request.PremiumAmount, request.Currency);

        return NoContent();
    }

    /// <summary>
    /// Declines a submission with a reason.
    /// </summary>
    [HttpPost("{id:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Decline(Guid id, [FromBody] DeclineSubmissionRequest request, CancellationToken cancellationToken)
    {
        var command = new DeclineSubmissionCommand(id, request.Reason);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to decline submission",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Declined submission {SubmissionId} with reason: {Reason}", id, request.Reason);

        return NoContent();
    }

    /// <summary>
    /// Requests additional information for a submission.
    /// </summary>
    [HttpPost("{id:guid}/request-info")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestInfo(Guid id, [FromBody] RequestInfoRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestInformationCommand(id, request.Message);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to request information",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Requested information for submission {SubmissionId}", id);

        return NoContent();
    }

    /// <summary>
    /// Binds a quoted submission, creating a policy in the external PAS system.
    /// </summary>
    [HttpPost("{id:guid}/bind")]
    [ProducesResponseType(typeof(BindSubmissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Bind(Guid id, CancellationToken cancellationToken)
    {
        var command = new BindSubmissionCommand(id);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to bind submission",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation(
            "Bound submission {SubmissionId} with policy number {PolicyNumber}",
            id,
            result.Value?.PolicyNumber);

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets submissions pending clearance review.
    /// </summary>
    [HttpGet("clearance-queue")]
    [ProducesResponseType(typeof(IReadOnlyList<SubmissionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClearanceQueue(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetClearanceQueueQuery(limit), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Overrides a failed clearance check on a submission.
    /// </summary>
    [HttpPost("{id:guid}/override-clearance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OverrideClearance(Guid id, [FromBody] OverrideClearanceRequest request, CancellationToken cancellationToken)
    {
        var command = new OverrideClearanceCommand(id, request.Reason, request.OverriddenByUserId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to override clearance",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Clearance overridden for submission {SubmissionId}", id);

        return NoContent();
    }

    /// <summary>
    /// Withdraws a submission.
    /// </summary>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawSubmissionRequest request, CancellationToken cancellationToken)
    {
        var command = new WithdrawSubmissionCommand(id, request.Reason);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to withdraw submission",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Withdrawn submission {SubmissionId} with reason: {Reason}", id, request.Reason);

        return NoContent();
    }

    /// <summary>
    /// Expires a submission.
    /// </summary>
    [HttpPost("{id:guid}/expire")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Expire(Guid id, [FromBody] ExpireSubmissionRequest request, CancellationToken cancellationToken)
    {
        var command = new ExpireSubmissionCommand(id, request.Reason);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to expire submission",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Expired submission {SubmissionId} with reason: {Reason}", id, request.Reason);

        return NoContent();
    }

    /// <summary>
    /// Gets data correction requests for a submission.
    /// </summary>
    [HttpGet("{id:guid}/corrections")]
    [ProducesResponseType(typeof(IReadOnlyList<DataCorrectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCorrections(Guid id, CancellationToken cancellationToken)
    {
        var corrections = await mediator.Send(new GetDataCorrectionsQuery(id), cancellationToken);
        return Ok(corrections);
    }

    /// <summary>
    /// Creates a data correction request for a submission.
    /// </summary>
    [HttpPost("{id:guid}/corrections")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCorrection(
        Guid id,
        [FromBody] CreateCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDataCorrectionCommand(
            id,
            request.Type,
            request.FieldName,
            request.CurrentValue,
            request.ProposedValue,
            request.Justification);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Submission.NotFound")
            {
                return NotFound();
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to create correction request",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Created correction request {CorrectionId} for submission {SubmissionId}", result.Value, id);

        return CreatedAtAction(nameof(GetCorrections), new { id }, result.Value);
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

public record CreateCorrectionRequest(
    DataCorrectionType Type,
    string FieldName,
    string? CurrentValue,
    string ProposedValue,
    string Justification);

public record QuoteSubmissionRequest(
    decimal PremiumAmount,
    string Currency = "USD");

public record DeclineSubmissionRequest(
    string Reason);

public record RequestInfoRequest(
    string Message);

public record OverrideClearanceRequest(
    string Reason,
    Guid OverriddenByUserId);

public record WithdrawSubmissionRequest(
    string? Reason);

public record ExpireSubmissionRequest(
    string? Reason);
