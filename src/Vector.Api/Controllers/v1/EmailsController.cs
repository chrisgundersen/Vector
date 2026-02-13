using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.EmailIntake.Commands;
using Vector.Application.EmailIntake.DTOs;
using Vector.Application.EmailIntake.Queries;

namespace Vector.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EmailsController(IMediator mediator, ILogger<EmailsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets an inbound email by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InboundEmailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var email = await mediator.Send(new GetInboundEmailQuery(id), cancellationToken);

        if (email is null)
        {
            return NotFound();
        }

        return Ok(email);
    }

    /// <summary>
    /// Processes a new inbound email.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessEmail([FromBody] ProcessEmailRequest request, CancellationToken cancellationToken)
    {
        var command = new ProcessInboundEmailCommand(
            request.TenantId,
            request.MailboxId,
            request.ExternalMessageId,
            request.FromAddress,
            request.Subject,
            request.BodyPreview,
            request.BodyContent,
            request.ReceivedAt);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to process email",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Processed inbound email {EmailId}", result.Value);

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Extracts an attachment from an email.
    /// </summary>
    [HttpPost("{emailId:guid}/attachments")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractAttachment(
        Guid emailId,
        [FromBody] ExtractAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ExtractAttachmentCommand(
            emailId,
            request.FileName,
            request.ContentType,
            Convert.FromBase64String(request.ContentBase64));

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to extract attachment",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Extracted attachment {AttachmentId} from email {EmailId}", result.Value, emailId);

        return Created($"/api/v1/emails/{emailId}/attachments/{result.Value}", result.Value);
    }
}

public record ProcessEmailRequest(
    Guid TenantId,
    string MailboxId,
    string ExternalMessageId,
    string FromAddress,
    string Subject,
    string BodyPreview,
    string BodyContent,
    DateTime ReceivedAt);

public record ExtractAttachmentRequest(
    string FileName,
    string ContentType,
    string ContentBase64);
