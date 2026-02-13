using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vector.Application.DocumentProcessing.Commands;
using Vector.Application.DocumentProcessing.DTOs;
using Vector.Application.DocumentProcessing.Queries;
using Vector.Application.DocumentProcessing.Services;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Api.Controllers.v1;

/// <summary>
/// Controller for managing document processing jobs.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/processing-jobs")]
public class ProcessingJobsController(
    IMediator mediator,
    IDataQualityScoringService scoringService,
    ILogger<ProcessingJobsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets a list of processing jobs.
    /// </summary>
    /// <param name="tenantId">Filter by tenant ID.</param>
    /// <param name="status">Filter by processing status.</param>
    /// <param name="limit">Maximum number of results (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProcessingJobSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] ProcessingStatus? status = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProcessingJobsQuery(tenantId, status, limit);
        var jobs = await mediator.Send(query, cancellationToken);
        return Ok(jobs);
    }

    /// <summary>
    /// Gets a processing job by ID.
    /// </summary>
    /// <param name="id">The processing job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProcessingJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var job = await mediator.Send(new GetProcessingJobQuery(id), cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        return Ok(job);
    }

    /// <summary>
    /// Gets the documents in a processing job.
    /// </summary>
    /// <param name="id">The processing job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:guid}/documents")]
    [ProducesResponseType(typeof(IReadOnlyList<ProcessedDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken cancellationToken)
    {
        var job = await mediator.Send(new GetProcessingJobQuery(id), cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        return Ok(job.Documents);
    }

    /// <summary>
    /// Gets a specific document from a processing job.
    /// </summary>
    /// <param name="id">The processing job ID.</param>
    /// <param name="documentId">The document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(typeof(ProcessedDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        var job = await mediator.Send(new GetProcessingJobQuery(id), cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        var document = job.Documents.FirstOrDefault(d => d.Id == documentId);

        if (document is null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    /// <summary>
    /// Gets the data quality score for a processing job.
    /// </summary>
    /// <param name="id">The processing job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:guid}/quality-score")]
    [ProducesResponseType(typeof(DataQualityScoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQualityScore(Guid id, CancellationToken cancellationToken)
    {
        // Get the full job from repository to calculate score
        var job = await mediator.Send(new GetProcessingJobQuery(id), cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        // Need the actual domain object for scoring
        var jobEntity = await GetProcessingJobEntityAsync(id, cancellationToken);
        if (jobEntity is null)
        {
            return NotFound();
        }

        var score = scoringService.CalculateJobScore(jobEntity);

        return Ok(new DataQualityScoreDto(
            score.OverallScore,
            score.CompletenessScore,
            score.ConfidenceScore,
            score.ValidationScore,
            score.CoverageScore,
            score.IsHighQuality,
            score.RequiresReview,
            score.Issues.Select(i => new DataQualityIssueDto(
                i.Type.ToString(),
                i.FieldName,
                i.Description,
                i.Severity.ToString())).ToList()));
    }

    /// <summary>
    /// Starts a new processing job for an inbound email.
    /// </summary>
    /// <param name="request">The start processing request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartProcessingJob(
        [FromBody] StartProcessingJobRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StartProcessingJobCommand(request.TenantId, request.InboundEmailId);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to start processing job",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation(
            "Started processing job {JobId} for email {EmailId}",
            result.Value,
            request.InboundEmailId);

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Processes all documents in a processing job.
    /// </summary>
    /// <param name="id">The processing job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessDocuments(Guid id, CancellationToken cancellationToken)
    {
        // First verify the job exists
        var existingJob = await mediator.Send(new GetProcessingJobQuery(id), cancellationToken);
        if (existingJob is null)
        {
            return NotFound();
        }

        var command = new ProcessDocumentsCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to process documents",
                Detail = result.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Processed documents for job {JobId}", id);

        return AcceptedAtAction(nameof(GetById), new { id }, null);
    }

    // Helper method to get the actual domain entity for scoring
    private async Task<Domain.DocumentProcessing.Aggregates.ProcessingJob?> GetProcessingJobEntityAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        // This is a workaround - ideally we'd have a separate query that returns the domain object
        // or the scoring service would work directly with the repository
        var serviceProvider = HttpContext.RequestServices;
        var repository = serviceProvider.GetRequiredService<Domain.DocumentProcessing.IProcessingJobRepository>();
        return await repository.GetByIdAsync(jobId, cancellationToken);
    }
}

/// <summary>
/// Request to start a processing job.
/// </summary>
public record StartProcessingJobRequest(Guid TenantId, Guid InboundEmailId);

/// <summary>
/// DTO for data quality score response.
/// </summary>
public record DataQualityScoreDto(
    int OverallScore,
    int CompletenessScore,
    int ConfidenceScore,
    int ValidationScore,
    int CoverageScore,
    bool IsHighQuality,
    bool RequiresReview,
    IReadOnlyList<DataQualityIssueDto> Issues);

/// <summary>
/// DTO for data quality issue.
/// </summary>
public record DataQualityIssueDto(
    string Type,
    string FieldName,
    string Description,
    string Severity);
