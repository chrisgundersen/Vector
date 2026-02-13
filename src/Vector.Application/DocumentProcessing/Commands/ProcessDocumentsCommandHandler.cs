using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;
using Vector.Domain.DocumentProcessing;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.DocumentProcessing.ValueObjects;

namespace Vector.Application.DocumentProcessing.Commands;

/// <summary>
/// Handler for ProcessDocumentsCommand. Processes all documents in a job through
/// classification, extraction, and validation phases.
/// </summary>
public sealed class ProcessDocumentsCommandHandler(
    IProcessingJobRepository processingJobRepository,
    IBlobStorageService blobStorageService,
    IDocumentIntelligenceService documentIntelligenceService,
    ILogger<ProcessDocumentsCommandHandler> logger) : IRequestHandler<ProcessDocumentsCommand, Result>
{
    private const string AttachmentContainer = "email-attachments";

    public async Task<Result> Handle(
        ProcessDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        var job = await processingJobRepository.GetByIdAsync(request.ProcessingJobId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Processing job {JobId} not found", request.ProcessingJobId);
            return Result.Failure(ProcessingJobErrors.DocumentNotFound);
        }

        if (job.Status is not (ProcessingStatus.Pending or ProcessingStatus.Classifying))
        {
            logger.LogWarning(
                "Processing job {JobId} is not in a valid state for processing. Current status: {Status}",
                job.Id,
                job.Status);
            return Result.Failure(ProcessingJobErrors.InvalidJobStatus);
        }

        if (job.Documents.Count == 0)
        {
            logger.LogWarning("Processing job {JobId} has no documents to process", job.Id);
            job.Fail("No documents to process");
            processingJobRepository.Update(job);
            return Result.Failure(ProcessingJobErrors.NoDocumentsToProcess);
        }

        try
        {
            // Phase 1: Classification
            await ClassifyDocumentsAsync(job, cancellationToken);
            processingJobRepository.Update(job);
            await processingJobRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Phase 2: Extraction
            await ExtractDocumentsAsync(job, cancellationToken);
            processingJobRepository.Update(job);
            await processingJobRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Phase 3: Validation
            ValidateDocuments(job);
            processingJobRepository.Update(job);
            await processingJobRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Complete the job
            job.Complete();
            processingJobRepository.Update(job);

            logger.LogInformation(
                "Completed processing job {JobId}. Total: {Total}, Successful: {Successful}, Failed: {Failed}",
                job.Id,
                job.Documents.Count,
                job.Documents.Count(d => d.Status == ProcessingStatus.Completed),
                job.Documents.Count(d => d.Status == ProcessingStatus.Failed));

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing documents for job {JobId}", job.Id);
            job.Fail(ex.Message);
            processingJobRepository.Update(job);
            return Result.Failure(new Error("ProcessingJob.ProcessingFailed", ex.Message));
        }
    }

    private async Task ClassifyDocumentsAsync(ProcessingJob job, CancellationToken cancellationToken)
    {
        job.StartClassification();

        logger.LogInformation(
            "Starting classification phase for job {JobId} with {DocumentCount} documents",
            job.Id,
            job.Documents.Count);

        foreach (var document in job.Documents)
        {
            try
            {
                await ClassifyDocumentAsync(job, document, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error classifying document {DocumentId}: {FileName}",
                    document.Id,
                    document.OriginalFileName);
                document.MarkAsFailed($"Classification failed: {ex.Message}");
            }
        }
    }

    private async Task ClassifyDocumentAsync(
        ProcessingJob job,
        ProcessedDocument document,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Classifying document {DocumentId}: {FileName}",
            document.Id,
            document.OriginalFileName);

        var blobName = GetBlobNameFromUrl(document.BlobStorageUrl);
        await using var stream = await blobStorageService.DownloadAsync(
            AttachmentContainer,
            blobName,
            cancellationToken);

        var result = await documentIntelligenceService.ClassifyDocumentAsync(
            stream,
            document.OriginalFileName,
            cancellationToken);

        job.OnDocumentClassified(document.Id, result.DocumentType, result.Confidence);

        logger.LogInformation(
            "Classified document {DocumentId} as {DocumentType} with confidence {Confidence:P}",
            document.Id,
            result.DocumentType,
            result.Confidence);
    }

    private async Task ExtractDocumentsAsync(ProcessingJob job, CancellationToken cancellationToken)
    {
        job.StartExtraction();

        var classifiedDocuments = job.Documents
            .Where(d => d.Status == ProcessingStatus.Classified)
            .ToList();

        logger.LogInformation(
            "Starting extraction phase for job {JobId} with {DocumentCount} classified documents",
            job.Id,
            classifiedDocuments.Count);

        foreach (var document in classifiedDocuments)
        {
            try
            {
                await ExtractDocumentAsync(job, document, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error extracting document {DocumentId}: {DocumentType}",
                    document.Id,
                    document.DocumentType);
                document.MarkAsFailed($"Extraction failed: {ex.Message}");
            }
        }
    }

    private async Task ExtractDocumentAsync(
        ProcessingJob job,
        ProcessedDocument document,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Extracting document {DocumentId}: {DocumentType}",
            document.Id,
            document.DocumentType);

        var blobName = GetBlobNameFromUrl(document.BlobStorageUrl);
        await using var stream = await blobStorageService.DownloadAsync(
            AttachmentContainer,
            blobName,
            cancellationToken);

        switch (document.DocumentType)
        {
            case DocumentType.Acord125:
            case DocumentType.Acord126:
            case DocumentType.Acord130:
            case DocumentType.Acord140:
            case DocumentType.Acord127:
            case DocumentType.Acord137:
                await ExtractAcordFormAsync(document, stream, cancellationToken);
                break;

            case DocumentType.LossRunReport:
                await ExtractLossRunAsync(document, stream, cancellationToken);
                break;

            case DocumentType.ExposureSchedule:
                await ExtractExposureScheduleAsync(document, stream, cancellationToken);
                break;

            case DocumentType.Unknown:
            case DocumentType.Other:
                logger.LogWarning(
                    "No extraction support for document type {DocumentType}. Marking as extracted.",
                    document.DocumentType);
                document.CompleteExtraction();
                break;

            default:
                logger.LogWarning(
                    "Unhandled document type {DocumentType} for extraction",
                    document.DocumentType);
                document.CompleteExtraction();
                break;
        }

        job.OnDocumentExtractionCompleted(document.Id);

        logger.LogInformation(
            "Extracted {FieldCount} fields from document {DocumentId}",
            document.ExtractedFields.Count,
            document.Id);
    }

    private async Task ExtractAcordFormAsync(
        ProcessedDocument document,
        Stream stream,
        CancellationToken cancellationToken)
    {
        var result = await documentIntelligenceService.ExtractAcordFormAsync(
            stream,
            document.DocumentType,
            cancellationToken);

        foreach (var (fieldName, fieldResult) in result.Fields)
        {
            var field = ExtractedField.Create(
                fieldName,
                fieldResult.Value,
                fieldResult.Confidence,
                fieldResult.BoundingBox,
                fieldResult.PageNumber);

            if (field.IsSuccess)
            {
                document.AddExtractedField(field.Value);
            }
        }

        document.CompleteExtraction();
    }

    private async Task ExtractLossRunAsync(
        ProcessedDocument document,
        Stream stream,
        CancellationToken cancellationToken)
    {
        var result = await documentIntelligenceService.ExtractLossRunAsync(stream, cancellationToken);

        // Add carrier name if available
        if (!string.IsNullOrEmpty(result.CarrierName))
        {
            AddField(document, "CarrierName", result.CarrierName, 0.85m);
        }

        // Add report date if available
        if (result.ReportDate.HasValue)
        {
            AddField(document, "ReportDate", result.ReportDate.Value.ToString("yyyy-MM-dd"), 0.85m);
        }

        // Add loss records
        for (var i = 0; i < result.Losses.Count; i++)
        {
            var loss = result.Losses[i];
            var prefix = $"Loss_{i + 1}_";

            if (loss.DateOfLoss.HasValue)
                AddField(document, $"{prefix}DateOfLoss", loss.DateOfLoss.Value.ToString("yyyy-MM-dd"), 0.85m);
            if (!string.IsNullOrEmpty(loss.ClaimNumber))
                AddField(document, $"{prefix}ClaimNumber", loss.ClaimNumber, 0.90m);
            if (!string.IsNullOrEmpty(loss.Description))
                AddField(document, $"{prefix}Description", loss.Description, 0.80m);
            if (loss.PaidAmount.HasValue)
                AddField(document, $"{prefix}PaidAmount", loss.PaidAmount.Value.ToString("F2"), 0.88m);
            if (loss.ReservedAmount.HasValue)
                AddField(document, $"{prefix}ReservedAmount", loss.ReservedAmount.Value.ToString("F2"), 0.87m);
            if (loss.IncurredAmount.HasValue)
                AddField(document, $"{prefix}IncurredAmount", loss.IncurredAmount.Value.ToString("F2"), 0.88m);
            if (!string.IsNullOrEmpty(loss.Status))
                AddField(document, $"{prefix}Status", loss.Status, 0.85m);
            if (!string.IsNullOrEmpty(loss.CoverageType))
                AddField(document, $"{prefix}CoverageType", loss.CoverageType, 0.82m);
        }

        AddField(document, "LossCount", result.Losses.Count.ToString(), 0.95m);

        document.CompleteExtraction();
    }

    private async Task ExtractExposureScheduleAsync(
        ProcessedDocument document,
        Stream stream,
        CancellationToken cancellationToken)
    {
        var result = await documentIntelligenceService.ExtractExposureScheduleAsync(stream, cancellationToken);

        // Add location records
        for (var i = 0; i < result.Locations.Count; i++)
        {
            var location = result.Locations[i];
            var prefix = $"Location_{location.LocationNumber ?? (i + 1)}_";

            if (!string.IsNullOrEmpty(location.Street1))
                AddField(document, $"{prefix}Street1", location.Street1, 0.90m);
            if (!string.IsNullOrEmpty(location.Street2))
                AddField(document, $"{prefix}Street2", location.Street2, 0.88m);
            if (!string.IsNullOrEmpty(location.City))
                AddField(document, $"{prefix}City", location.City, 0.92m);
            if (!string.IsNullOrEmpty(location.State))
                AddField(document, $"{prefix}State", location.State, 0.95m);
            if (!string.IsNullOrEmpty(location.PostalCode))
                AddField(document, $"{prefix}PostalCode", location.PostalCode, 0.88m);
            if (!string.IsNullOrEmpty(location.BuildingDescription))
                AddField(document, $"{prefix}Description", location.BuildingDescription, 0.80m);
            if (location.BuildingValue.HasValue)
                AddField(document, $"{prefix}BuildingValue", location.BuildingValue.Value.ToString("F2"), 0.85m);
            if (location.ContentsValue.HasValue)
                AddField(document, $"{prefix}ContentsValue", location.ContentsValue.Value.ToString("F2"), 0.84m);
            if (location.BusinessIncomeValue.HasValue)
                AddField(document, $"{prefix}BusinessIncomeValue", location.BusinessIncomeValue.Value.ToString("F2"), 0.83m);
            if (!string.IsNullOrEmpty(location.ConstructionType))
                AddField(document, $"{prefix}ConstructionType", location.ConstructionType, 0.80m);
            if (location.YearBuilt.HasValue)
                AddField(document, $"{prefix}YearBuilt", location.YearBuilt.Value.ToString(), 0.82m);
            if (location.SquareFootage.HasValue)
                AddField(document, $"{prefix}SquareFootage", location.SquareFootage.Value.ToString(), 0.80m);
        }

        AddField(document, "LocationCount", result.Locations.Count.ToString(), 0.95m);

        document.CompleteExtraction();
    }

    private static void AddField(ProcessedDocument document, string fieldName, string value, decimal confidence)
    {
        var field = ExtractedField.Create(fieldName, value, confidence);
        if (field.IsSuccess)
        {
            document.AddExtractedField(field.Value);
        }
    }

    private void ValidateDocuments(ProcessingJob job)
    {
        job.StartValidation();

        var extractedDocuments = job.Documents
            .Where(d => d.Status == ProcessingStatus.Extracted)
            .ToList();

        logger.LogInformation(
            "Starting validation phase for job {JobId} with {DocumentCount} extracted documents",
            job.Id,
            extractedDocuments.Count);

        foreach (var document in extractedDocuments)
        {
            ValidateDocument(document);
        }
    }

    private void ValidateDocument(ProcessedDocument document)
    {
        switch (document.DocumentType)
        {
            case DocumentType.Acord125:
                ValidateAcord125(document);
                break;

            case DocumentType.Acord126:
                ValidateAcord126(document);
                break;

            case DocumentType.Acord130:
                ValidateAcord130(document);
                break;

            case DocumentType.Acord140:
                ValidateAcord140(document);
                break;

            case DocumentType.LossRunReport:
                ValidateLossRun(document);
                break;

            case DocumentType.ExposureSchedule:
                ValidateExposureSchedule(document);
                break;

            default:
                // No specific validation for other document types
                break;
        }

        // Check for low confidence fields that need review
        var lowConfidenceFields = document.ExtractedFields
            .Where(f => f.Confidence.RequiresReview)
            .ToList();

        if (lowConfidenceFields.Count > 5)
        {
            document.AddValidationError(
                $"Multiple fields ({lowConfidenceFields.Count}) have low extraction confidence and require manual review.");
        }

        document.CompleteValidation();

        logger.LogDebug(
            "Validated document {DocumentId}. Errors: {ErrorCount}, Final Status: {Status}",
            document.Id,
            document.ValidationErrors.Count,
            document.Status);
    }

    private static void ValidateAcord125(ProcessedDocument document)
    {
        if (document.GetFieldValue("InsuredName") is null)
        {
            document.AddValidationError("Insured name is required but was not extracted.");
        }

        if (document.GetFieldValue("EffectiveDate") is null)
        {
            document.AddValidationError("Policy effective date is required but was not extracted.");
        }

        // Check for address completeness
        var hasAddress = document.GetFieldValue("InsuredAddress") is not null ||
                         document.GetFieldValue("InsuredCity") is not null;
        if (!hasAddress)
        {
            document.AddValidationError("Insured address information is incomplete.");
        }
    }

    private static void ValidateAcord126(ProcessedDocument document)
    {
        if (document.GetFieldValue("InsuredName") is null)
        {
            document.AddValidationError("Insured name is required for GL section.");
        }

        // Check for GL limit
        var hasGLLimit = document.GetFieldValue("GLLimit") is not null ||
                         document.GetFieldValue("GeneralLiabilityLimit") is not null;
        if (!hasGLLimit)
        {
            document.AddValidationError("General liability limit was not extracted.");
        }
    }

    private static void ValidateAcord130(ProcessedDocument document)
    {
        if (document.GetFieldValue("InsuredName") is null)
        {
            document.AddValidationError("Insured name is required for Workers Comp application.");
        }

        // Check for state
        if (document.GetFieldValue("State") is null && document.GetFieldValue("InsuredState") is null)
        {
            document.AddValidationError("State is required for Workers Compensation.");
        }
    }

    private static void ValidateAcord140(ProcessedDocument document)
    {
        if (document.GetFieldValue("InsuredName") is null)
        {
            document.AddValidationError("Insured name is required for Property section.");
        }

        // Check for property values
        var hasPropertyValue = document.GetFieldValue("PropertyLimit") is not null ||
                               document.GetFieldValue("BuildingValue") is not null ||
                               document.GetFieldValue("TotalInsuredValue") is not null;
        if (!hasPropertyValue)
        {
            document.AddValidationError("Property values were not extracted.");
        }
    }

    private static void ValidateLossRun(ProcessedDocument document)
    {
        var lossCountField = document.GetFieldValue("LossCount");
        if (lossCountField is null || lossCountField == "0")
        {
            // This may be valid (no losses) - just a warning
            if (!document.ExtractedFields.Any())
            {
                document.AddValidationError("No loss data could be extracted from the loss run report.");
            }
        }
    }

    private static void ValidateExposureSchedule(ProcessedDocument document)
    {
        var locationCountField = document.GetFieldValue("LocationCount");
        if (locationCountField is null || locationCountField == "0")
        {
            document.AddValidationError("No location data could be extracted from the exposure schedule.");
        }
    }

    private static string GetBlobNameFromUrl(string url)
    {
        // Extract blob name from URL
        // Format: file:///path/to/blob or https://storage.blob.core.windows.net/container/blobname
        if (url.StartsWith("file:///"))
        {
            var path = url[8..];
            var containerIndex = path.IndexOf("email-attachments", StringComparison.OrdinalIgnoreCase);
            if (containerIndex >= 0)
            {
                return path[(containerIndex + "email-attachments/".Length)..].Replace('\\', '/');
            }
            // Return the filename portion
            return Path.GetFileName(path);
        }

        var uri = new Uri(url);
        var segments = uri.Segments;
        return string.Join("", segments.Skip(2)); // Skip container segment
    }
}
