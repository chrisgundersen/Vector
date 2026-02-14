using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.Common.Interfaces;
using Vector.Application.EmailIntake.Commands;
using Vector.Infrastructure.Email;

namespace Vector.Api.Controllers;

/// <summary>
/// Controller for simulating emails and testing the system locally.
/// Only available when EnableSimulationEndpoints is set to true in configuration.
/// </summary>
[ApiController]
[Route("api/simulation")]
[AllowAnonymous]
public class SimulationController : ControllerBase
{
    private readonly ISimulatedEmailService? _simulatedEmailService;
    private readonly IMediator _mediator;
    private readonly ILogger<SimulationController> _logger;
    private readonly IConfiguration _configuration;

    public SimulationController(
        IMediator mediator,
        ILogger<SimulationController> logger,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
        _simulatedEmailService = emailService as ISimulatedEmailService;
    }

    /// <summary>
    /// Checks if simulation endpoints are enabled.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SimulationStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        var enabled = _configuration.GetValue<bool>("EnableSimulationEndpoints");
        var hasSimulatedService = _simulatedEmailService is not null;

        return Ok(new SimulationStatusResponse(
            enabled && hasSimulatedService,
            hasSimulatedService ? "SimulatedEmailService" : "ProductionEmailService",
            enabled ? "Simulation endpoints enabled" : "Simulation endpoints disabled in configuration"));
    }

    /// <summary>
    /// Sends a simulated email to the system for processing.
    /// </summary>
    [HttpPost("emails")]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult SendSimulatedEmail([FromBody] SendEmailRequest request)
    {
        if (!IsSimulationEnabled())
        {
            return ServiceUnavailable("Simulation endpoints are not enabled");
        }

        var simRequest = new SimulatedEmailRequest
        {
            FromAddress = request.FromAddress ?? "producer@example.com",
            FromName = request.FromName,
            Subject = request.Subject ?? "New Insurance Submission",
            Body = request.Body ?? "Please find the attached submission documents for review.",
            IsHtml = request.IsHtml,
            Attachments = request.Attachments?.Select(a => new SimulatedAttachment
            {
                FileName = a.FileName ?? "ACORD125.pdf",
                ContentType = a.ContentType,
                Base64Content = a.Base64Content
            }).ToList()
        };

        var messageId = _simulatedEmailService!.AddSimulatedEmail(simRequest);

        _logger.LogInformation(
            "Simulated email sent: {MessageId} from {From} with subject '{Subject}'",
            messageId, request.FromAddress, request.Subject);

        return CreatedAtAction(nameof(GetPendingEmails), new { }, new SendEmailResponse(
            messageId,
            "Email added to simulation queue",
            request.Attachments?.Count ?? 0));
    }

    /// <summary>
    /// Sends a sample ACORD submission email with attachments.
    /// </summary>
    [HttpPost("emails/sample-submission")]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult SendSampleSubmission([FromQuery] string? producerName = null, [FromQuery] string? insuredName = null)
    {
        if (!IsSimulationEnabled())
        {
            return ServiceUnavailable("Simulation endpoints are not enabled");
        }

        var producer = producerName ?? "ABC Insurance Agency";
        var insured = insuredName ?? $"Test Company {DateTime.Now:yyyyMMddHHmmss}";

        var simRequest = new SimulatedEmailRequest
        {
            FromAddress = "submissions@abcinsurance.com",
            FromName = producer,
            Subject = $"New Submission - {insured}",
            Body = $@"Dear Underwriting Team,

Please find attached the submission documents for our client:

Insured: {insured}
Coverage Requested: General Liability, Property
Effective Date: {DateTime.Today.AddMonths(1):MM/dd/yyyy}

Attached Documents:
- ACORD 125 (Commercial Insurance Application)
- ACORD 126 (Commercial General Liability Section)
- Loss Run Report (5 years)
- Statement of Values

Please let me know if you need any additional information.

Best regards,
{producer}",
            IsHtml = false,
            Attachments =
            [
                new SimulatedAttachment { FileName = "ACORD125_Application.pdf", ContentType = "application/pdf" },
                new SimulatedAttachment { FileName = "ACORD126_GL_Section.pdf", ContentType = "application/pdf" },
                new SimulatedAttachment { FileName = "LossRun_5Year.pdf", ContentType = "application/pdf" },
                new SimulatedAttachment { FileName = "StatementOfValues.xlsx", ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
            ]
        };

        var messageId = _simulatedEmailService!.AddSimulatedEmail(simRequest);

        _logger.LogInformation(
            "Sample submission email sent: {MessageId} for {Insured}",
            messageId, insured);

        return CreatedAtAction(nameof(GetPendingEmails), new { }, new SendEmailResponse(
            messageId,
            $"Sample submission email created for '{insured}'",
            4));
    }

    /// <summary>
    /// Gets all pending simulated emails (not yet processed).
    /// </summary>
    [HttpGet("emails/pending")]
    [ProducesResponseType(typeof(EmailListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetPendingEmails()
    {
        if (!IsSimulationEnabled())
        {
            return ServiceUnavailable("Simulation endpoints are not enabled");
        }

        var emails = _simulatedEmailService!.GetPendingEmails();

        return Ok(new EmailListResponse(
            emails.Select(e => new EmailSummary(
                e.MessageId,
                e.FromAddress,
                e.Subject,
                e.ReceivedDateTime,
                e.AttachmentCount,
                "Pending")).ToList(),
            emails.Count));
    }

    /// <summary>
    /// Gets all processed simulated emails.
    /// </summary>
    [HttpGet("emails/processed")]
    [ProducesResponseType(typeof(EmailListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetProcessedEmails()
    {
        if (!IsSimulationEnabled())
        {
            return ServiceUnavailable("Simulation endpoints are not enabled");
        }

        var emails = _simulatedEmailService!.GetProcessedEmails();

        return Ok(new EmailListResponse(
            emails.Select(e => new EmailSummary(
                e.MessageId,
                e.FromAddress,
                e.Subject,
                e.ReceivedDateTime,
                e.AttachmentCount,
                "Processed")).ToList(),
            emails.Count));
    }

    /// <summary>
    /// Triggers processing of pending emails (simulates the email polling worker).
    /// </summary>
    [HttpPost("process-emails")]
    [ProducesResponseType(typeof(ProcessEmailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ProcessPendingEmails([FromQuery] Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (!IsSimulationEnabled())
        {
            return ServiceUnavailable("Simulation endpoints are not enabled");
        }

        var effectiveTenantId = tenantId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var pendingEmails = _simulatedEmailService!.GetPendingEmails();
        var processedCount = 0;
        var errors = new List<string>();

        foreach (var email in pendingEmails)
        {
            try
            {
                // Create a processing command for this email
                var command = new ProcessInboundEmailCommand(
                    effectiveTenantId,
                    "submissions@vector.local",
                    email.MessageId,
                    email.FromAddress,
                    email.Subject,
                    email.BodyPreview,
                    email.BodyContent,
                    email.ReceivedDateTime);

                var result = await _mediator.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    processedCount++;
                    _logger.LogInformation(
                        "Processed simulated email {MessageId}, created InboundEmail {InboundEmailId}",
                        email.MessageId, result.Value);
                }
                else
                {
                    errors.Add($"{email.MessageId}: {result.Error.Description}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{email.MessageId}: {ex.Message}");
                _logger.LogError(ex, "Error processing simulated email {MessageId}", email.MessageId);
            }
        }

        return Ok(new ProcessEmailsResponse(
            pendingEmails.Count,
            processedCount,
            errors));
    }

    /// <summary>
    /// Clears all simulated emails (pending and processed).
    /// </summary>
    [HttpDelete("emails")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult ClearAllEmails()
    {
        if (!IsSimulationEnabled())
        {
            return ServiceUnavailable("Simulation endpoints are not enabled");
        }

        _simulatedEmailService!.ClearAll();
        _logger.LogInformation("All simulated emails cleared");

        return NoContent();
    }

    private bool IsSimulationEnabled()
    {
        var enabled = _configuration.GetValue<bool>("EnableSimulationEndpoints");
        return enabled && _simulatedEmailService is not null;
    }

    private ObjectResult ServiceUnavailable(string message)
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
        {
            Title = "Simulation Not Available",
            Detail = message,
            Status = StatusCodes.Status503ServiceUnavailable
        });
    }
}

// Request/Response DTOs

public record SendEmailRequest
{
    public string? FromAddress { get; init; }
    public string? FromName { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public bool IsHtml { get; init; }
    public List<AttachmentRequest>? Attachments { get; init; }
}

public record AttachmentRequest
{
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public string? Base64Content { get; init; }
}

public record SendEmailResponse(string MessageId, string Message, int AttachmentCount);

public record SimulationStatusResponse(bool Enabled, string EmailServiceType, string Message);

public record EmailListResponse(List<EmailSummary> Emails, int TotalCount);

public record EmailSummary(
    string MessageId,
    string FromAddress,
    string Subject,
    DateTime ReceivedAt,
    int AttachmentCount,
    string Status);

public record ProcessEmailsResponse(int TotalPending, int Processed, List<string> Errors);
