using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.EmailIntake;

namespace Vector.Application.DocumentProcessing.Commands;

/// <summary>
/// Handler for StartProcessingJobCommand.
/// </summary>
public sealed class StartProcessingJobCommandHandler(
    IInboundEmailRepository emailRepository,
    IProcessingJobRepository processingJobRepository,
    ILogger<StartProcessingJobCommandHandler> logger) : IRequestHandler<StartProcessingJobCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        StartProcessingJobCommand request,
        CancellationToken cancellationToken)
    {
        // Validate the inbound email exists
        var email = await emailRepository.GetByIdAsync(request.InboundEmailId, cancellationToken);
        if (email is null)
        {
            logger.LogWarning(
                "Cannot start processing job: Inbound email {EmailId} not found",
                request.InboundEmailId);
            return Result.Failure<Guid>(ProcessingJobErrors.InboundEmailNotFound);
        }

        // Check if a processing job already exists for this email
        var existingJob = await processingJobRepository.GetByInboundEmailIdAsync(
            request.InboundEmailId,
            cancellationToken);

        if (existingJob is not null)
        {
            logger.LogWarning(
                "Processing job {JobId} already exists for email {EmailId}",
                existingJob.Id,
                request.InboundEmailId);
            return Result.Failure<Guid>(ProcessingJobErrors.JobAlreadyExists);
        }

        // Create the processing job
        var job = ProcessingJob.Create(request.TenantId, request.InboundEmailId);

        // Add documents from email attachments
        foreach (var attachment in email.Attachments)
        {
            var document = job.AddDocument(
                attachment.Id,
                attachment.Metadata.FileName,
                attachment.BlobStorageUrl);

            logger.LogDebug(
                "Added document {DocumentId} for attachment {AttachmentId}: {FileName}",
                document.Id,
                attachment.Id,
                attachment.Metadata.FileName);
        }

        await processingJobRepository.AddAsync(job, cancellationToken);

        // Start processing on the email
        email.StartProcessing();
        emailRepository.Update(email);

        logger.LogInformation(
            "Created processing job {JobId} for email {EmailId} with {DocumentCount} documents",
            job.Id,
            request.InboundEmailId,
            job.Documents.Count);

        return Result.Success(job.Id);
    }
}

public static class ProcessingJobErrors
{
    public static readonly Error InboundEmailNotFound = new(
        "ProcessingJob.InboundEmailNotFound",
        "The specified inbound email was not found.");

    public static readonly Error JobAlreadyExists = new(
        "ProcessingJob.AlreadyExists",
        "A processing job already exists for this email.");

    public static readonly Error NoDocumentsToProcess = new(
        "ProcessingJob.NoDocuments",
        "No documents to process.");

    public static readonly Error DocumentNotFound = new(
        "ProcessingJob.DocumentNotFound",
        "The specified document was not found in the processing job.");

    public static readonly Error InvalidJobStatus = new(
        "ProcessingJob.InvalidStatus",
        "The processing job is not in a valid state for this operation.");
}
