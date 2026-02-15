using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.Common.Interfaces;
using Vector.Application.EmailIntake.Commands;
using Vector.Application.Submissions.Commands;
using Vector.Domain.Common;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;
using Vector.Infrastructure.Email;
using Vector.Infrastructure.Persistence;

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
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SimulationController(
        IMediator mediator,
        ILogger<SimulationController> logger,
        IConfiguration configuration,
        IEmailService emailService,
        ISubmissionRepository submissionRepository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
        _simulatedEmailService = emailService as ISimulatedEmailService;
        _submissionRepository = submissionRepository;
        _unitOfWork = unitOfWork;
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
                    _simulatedEmailService.MarkAsProcessed(email.MessageId);
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
    /// Creates test submissions directly (bypasses email/document processing pipeline).
    /// Useful for testing the underwriting dashboard.
    /// </summary>
    [HttpPost("submissions")]
    [ProducesResponseType(typeof(CreateSubmissionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateTestSubmissions(
        [FromQuery] int count = 5,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsSimulationEnabled())
        {
            return ServiceUnavailable("Simulation endpoints are not enabled");
        }

        var effectiveTenantId = tenantId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var createdIds = new List<Guid>();
        var errors = new List<string>();
        var random = new Random();

        for (var i = 0; i < count; i++)
        {
            try
            {
                var scenario = TestScenarios[random.Next(TestScenarios.Length)];
                var producer = Producers[random.Next(Producers.Length)];
                var underwriter = Underwriters[random.Next(Underwriters.Length)];
                var insuredName = $"{scenario.InsuredPrefix} {GenerateCompanySuffix(random)} {DateTime.Now:HHmmss}-{i + 1}";

                var submissionNumber = await _submissionRepository.GenerateSubmissionNumberAsync(effectiveTenantId, cancellationToken);

                var submissionResult = Submission.Create(effectiveTenantId, submissionNumber, insuredName);
                if (submissionResult.IsFailure)
                {
                    errors.Add($"Failed to create submission: {submissionResult.Error.Description}");
                    continue;
                }

                var submission = submissionResult.Value;

                // Set producer info
                submission.UpdateProducerInfo(producer.Id, producer.Name, producer.Email);

                // Set policy dates
                submission.UpdatePolicyDates(
                    DateTime.UtcNow.AddMonths(1).Date,
                    DateTime.UtcNow.AddMonths(13).Date);

                // Add coverages
                AddCoverages(submission, scenario, random);

                // Add locations with values
                AddLocations(submission, scenario, random);

                // Add loss history
                AddLossHistory(submission, scenario, random);

                // Set scores based on scenario
                submission.UpdateScores(
                    scenario.BaseAppetiteScore + random.Next(-10, 11),
                    scenario.BaseWinnabilityScore + random.Next(-10, 11),
                    scenario.BaseDataQualityScore + random.Next(-5, 6));

                // Mark as received
                submission.MarkAsReceived();

                // Assign to underwriter (based on producer pairing)
                submission.AssignToUnderwriter(underwriter.Id, underwriter.Name);

                await _submissionRepository.AddAsync(submission, cancellationToken);
                createdIds.Add(submission.Id);

                _logger.LogInformation(
                    "Created test submission {SubmissionNumber} for {InsuredName} with {CoverageCount} coverages, {LocationCount} locations, {LossCount} losses",
                    submissionNumber, insuredName, submission.Coverages.Count, submission.Locations.Count, submission.LossHistory.Count);
            }
            catch (Exception ex)
            {
                errors.Add($"Error creating submission {i + 1}: {ex.Message}");
                _logger.LogError(ex, "Error creating test submission {Index}", i + 1);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new CreateSubmissionsResponse(
            count,
            createdIds.Count,
            createdIds,
            errors));
    }

    #region Test Data Generation

    private static readonly (Guid Id, string Name, string Email)[] Producers =
    [
        (Guid.Parse("33333333-3333-3333-3333-333333333333"), "ABC Insurance Agency", "submissions@abcinsurance.com"),
        (Guid.Parse("44444444-4444-4444-4444-444444444444"), "XYZ Brokers Inc", "newbusiness@xyzbrokers.com"),
        (Guid.Parse("33333333-3333-3333-3333-333333333334"), "Marsh McLennan", "submissions@marsh.com"),
        (Guid.Parse("33333333-3333-3333-3333-333333333335"), "Aon Risk Solutions", "newbusiness@aon.com")
    ];

    private static readonly (Guid Id, string Name)[] Underwriters =
    [
        (Guid.Parse("11111111-1111-1111-1111-111111111111"), "John Smith"),
        (Guid.Parse("22222222-2222-2222-2222-222222222222"), "Jane Doe"),
        (Guid.Parse("11111111-1111-1111-1111-111111111112"), "Mike Johnson")
    ];

    private record TestScenario(
        string InsuredPrefix,
        CoverageType[] Coverages,
        string OccupancyType,
        int BaseAppetiteScore,
        int BaseWinnabilityScore,
        int BaseDataQualityScore,
        int MaxLosses);

    private static readonly TestScenario[] TestScenarios =
    [
        new("Advanced Manufacturing", [CoverageType.GeneralLiability, CoverageType.PropertyDamage, CoverageType.ProductsCompleted], "Manufacturing Facility", 80, 75, 85, 2),
        new("Precision Logistics", [CoverageType.GeneralLiability, CoverageType.PropertyDamage, CoverageType.Auto], "Distribution Center", 75, 70, 80, 3),
        new("Regional Healthcare", [CoverageType.GeneralLiability, CoverageType.ProfessionalLiability, CoverageType.PropertyDamage], "Medical Office", 85, 80, 90, 1),
        new("Metro Retail", [CoverageType.GeneralLiability, CoverageType.PropertyDamage], "Retail Store", 70, 65, 75, 2),
        new("Tech Solutions", [CoverageType.GeneralLiability, CoverageType.ProfessionalLiability, CoverageType.Cyber], "Office - Technology", 82, 78, 88, 1),
        new("Industrial Services", [CoverageType.GeneralLiability, CoverageType.WorkersCompensation, CoverageType.Auto], "Industrial Facility", 65, 60, 70, 4),
        new("Professional Partners", [CoverageType.GeneralLiability, CoverageType.ProfessionalLiability, CoverageType.Cyber], "Office - Professional", 88, 85, 92, 1),
        new("Coastal Properties", [CoverageType.PropertyDamage, CoverageType.GeneralLiability], "Mixed Use Commercial", 72, 68, 78, 2)
    ];

    private static readonly string[] CompanySuffixes = ["Inc", "LLC", "Corp", "Group", "Holdings", "Partners", "Services", "Solutions", "Industries", "Enterprises"];

    private static readonly string[][] CityStateZip =
    [
        ["New York", "NY", "10001"],
        ["Los Angeles", "CA", "90001"],
        ["Chicago", "IL", "60601"],
        ["Houston", "TX", "77001"],
        ["Phoenix", "AZ", "85001"],
        ["Philadelphia", "PA", "19101"],
        ["Dallas", "TX", "75201"],
        ["Atlanta", "GA", "30301"],
        ["Miami", "FL", "33101"],
        ["Seattle", "WA", "98101"]
    ];

    private static string GenerateCompanySuffix(Random random) => CompanySuffixes[random.Next(CompanySuffixes.Length)];

    private static void AddCoverages(Submission submission, TestScenario scenario, Random random)
    {
        foreach (var coverageType in scenario.Coverages)
        {
            var coverage = submission.AddCoverage(coverageType);
            var (limit, deductible) = GetCoverageLimits(coverageType, random);
            coverage.UpdateRequestedLimit(limit);
            coverage.UpdateRequestedDeductible(deductible);
        }
    }

    private static (Money limit, Money deductible) GetCoverageLimits(CoverageType type, Random random)
    {
        return type switch
        {
            CoverageType.GeneralLiability => (Money.FromDecimal(1_000_000 * (1 + random.Next(5)), "USD"), Money.FromDecimal(10_000 * (1 + random.Next(5)), "USD")),
            CoverageType.PropertyDamage => (Money.FromDecimal(5_000_000 * (1 + random.Next(4)), "USD"), Money.FromDecimal(25_000 * (1 + random.Next(4)), "USD")),
            CoverageType.WorkersCompensation => (Money.FromDecimal(1_000_000, "USD"), Money.FromDecimal(0, "USD")),
            CoverageType.Auto => (Money.FromDecimal(1_000_000, "USD"), Money.FromDecimal(5_000 * (1 + random.Next(3)), "USD")),
            CoverageType.ProfessionalLiability => (Money.FromDecimal(2_000_000 * (1 + random.Next(3)), "USD"), Money.FromDecimal(50_000 * (1 + random.Next(3)), "USD")),
            CoverageType.Cyber => (Money.FromDecimal(2_000_000 * (1 + random.Next(5)), "USD"), Money.FromDecimal(25_000 * (1 + random.Next(4)), "USD")),
            CoverageType.ProductsCompleted => (Money.FromDecimal(2_000_000, "USD"), Money.FromDecimal(25_000, "USD")),
            CoverageType.Umbrella => (Money.FromDecimal(5_000_000 * (1 + random.Next(4)), "USD"), Money.FromDecimal(10_000, "USD")),
            _ => (Money.FromDecimal(1_000_000, "USD"), Money.FromDecimal(10_000, "USD"))
        };
    }

    private static void AddLocations(Submission submission, TestScenario scenario, Random random)
    {
        var locationCount = 1 + random.Next(3); // 1-3 locations
        for (var i = 0; i < locationCount; i++)
        {
            var cityInfo = CityStateZip[random.Next(CityStateZip.Length)];
            var streetNum = 100 + random.Next(9900);
            var streets = new[] { "Main St", "Commerce Dr", "Industrial Blvd", "Business Park Way", "Corporate Center", "Tech Drive" };

            var addressResult = Address.Create(
                $"{streetNum} {streets[random.Next(streets.Length)]}",
                i == 0 ? null : $"Suite {100 + random.Next(900)}",
                cityInfo[0],
                cityInfo[1],
                cityInfo[2],
                "US");

            if (addressResult.IsFailure) continue;

            var location = submission.AddLocation(addressResult.Value);
            location.UpdateOccupancyType(scenario.OccupancyType);
            location.UpdateSquareFootage(10000 + random.Next(190000));
            location.UpdateConstruction(
                random.Next(2) == 0 ? "Fire Resistive" : "Non-Combustible",
                1980 + random.Next(45),
                1 + random.Next(5));

            var buildingValue = 1_000_000m + random.Next(20) * 500_000m;
            var contentsValue = buildingValue * (0.2m + (decimal)random.NextDouble() * 0.3m);
            var biValue = buildingValue * (0.1m + (decimal)random.NextDouble() * 0.2m);

            location.UpdateValues(
                Money.FromDecimal(buildingValue, "USD"),
                Money.FromDecimal(contentsValue, "USD"),
                Money.FromDecimal(biValue, "USD"));

            location.UpdateProtection(
                random.Next(3) > 0, // 67% have sprinklers
                random.Next(4) > 0, // 75% have fire alarm
                random.Next(2) > 0, // 50% have security
                (1 + random.Next(5)).ToString());
        }
    }

    private static void AddLossHistory(Submission submission, TestScenario scenario, Random random)
    {
        var lossCount = random.Next(scenario.MaxLosses + 1);
        var coverages = scenario.Coverages;

        for (var i = 0; i < lossCount; i++)
        {
            var monthsAgo = 6 + random.Next(54); // 6 months to 5 years ago
            var dateOfLoss = DateTime.UtcNow.AddMonths(-monthsAgo);
            var coverageType = coverages[random.Next(coverages.Length)];

            var descriptions = GetLossDescriptions(coverageType);
            var description = descriptions[random.Next(descriptions.Length)];

            var loss = submission.AddLoss(dateOfLoss, description);
            loss.UpdateClaimInfo(
                $"CLM-{dateOfLoss.Year}-{random.Next(1000, 9999):D4}",
                coverageType,
                "Prior Insurance Carrier");

            var paidAmount = 5000m + random.Next(100) * 1000m;
            var isOpen = monthsAgo < 12 && random.Next(3) == 0; // Recent claims may be open
            var reservedAmount = isOpen ? paidAmount * (0.5m + (decimal)random.NextDouble()) : 0m;

            loss.UpdateAmounts(
                Money.FromDecimal(paidAmount, "USD"),
                Money.FromDecimal(reservedAmount, "USD"),
                null);

            loss.UpdateStatus(isOpen ? LossStatus.Open : LossStatus.ClosedWithPayment);
        }
    }

    private static string[] GetLossDescriptions(CoverageType type)
    {
        return type switch
        {
            CoverageType.GeneralLiability => ["Slip and fall incident", "Customer injury on premises", "Third-party property damage", "Advertising injury claim"],
            CoverageType.PropertyDamage => ["Water damage from pipe burst", "Fire damage to equipment", "Roof leak damage", "Storm damage", "Vandalism"],
            CoverageType.WorkersCompensation => ["Employee back injury", "Repetitive strain injury", "Slip and fall at work", "Equipment-related injury"],
            CoverageType.Auto => ["Delivery vehicle accident", "Rear-end collision", "Parking lot incident", "Third-party vehicle damage"],
            CoverageType.ProfessionalLiability => ["Professional negligence claim", "Errors and omissions", "Failure to deliver services"],
            CoverageType.Cyber => ["Data breach incident", "Ransomware attack", "System outage", "Privacy violation"],
            CoverageType.ProductsCompleted => ["Product defect claim", "Completed work failure", "Product recall"],
            _ => ["General claim incident"]
        };
    }

    #endregion

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

public record CreateSubmissionsResponse(int Requested, int Created, List<Guid> SubmissionIds, List<string> Errors);
