using Vector.Application.Common.Interfaces;
using Vector.Domain.DocumentProcessing;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.DocumentProcessing.ValueObjects;
using Vector.Domain.EmailIntake;

namespace Vector.Worker.Services;

/// <summary>
/// Background worker that processes documents from inbound emails.
/// </summary>
public class DocumentProcessingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentProcessingWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(
        configuration.GetValue("DocumentProcessing:IntervalSeconds", 30));

    private readonly int _batchSize = configuration.GetValue("DocumentProcessing:BatchSize", 5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Document Processing Worker starting. Interval: {Interval}s, BatchSize: {BatchSize}",
            _processingInterval.TotalSeconds, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during document processing cycle");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IInboundEmailRepository>();
        var processingJobRepository = scope.ServiceProvider.GetRequiredService<IProcessingJobRepository>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        var documentIntelligence = scope.ServiceProvider.GetRequiredService<IDocumentIntelligenceService>();

        // Get pending emails (this is a simplified implementation)
        // In production, you'd want to use a proper queue
        var pendingJobs = await processingJobRepository.GetByStatusAsync(
            ProcessingStatus.Pending, _batchSize, cancellationToken);

        if (pendingJobs.Count == 0)
        {
            logger.LogDebug("No pending processing jobs found");
            return;
        }

        logger.LogInformation("Found {Count} pending processing jobs", pendingJobs.Count);

        foreach (var job in pendingJobs)
        {
            try
            {
                await ProcessJobAsync(job, blobStorage, documentIntelligence, processingJobRepository, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing job {JobId}", job.Id);
                job.Fail(ex.Message);
                processingJobRepository.Update(job);
                await processingJobRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ProcessJobAsync(
        ProcessingJob job,
        IBlobStorageService blobStorage,
        IDocumentIntelligenceService documentIntelligence,
        IProcessingJobRepository processingJobRepository,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing job {JobId} with {DocumentCount} documents",
            job.Id, job.Documents.Count);

        // Classification phase
        job.StartClassification();
        processingJobRepository.Update(job);
        await processingJobRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var document in job.Documents)
        {
            try
            {
                await ClassifyDocumentAsync(document, blobStorage, documentIntelligence, job, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error classifying document {DocumentId}", document.Id);
                document.MarkAsFailed($"Classification failed: {ex.Message}");
            }
        }

        // Extraction phase
        job.StartExtraction();
        processingJobRepository.Update(job);
        await processingJobRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var document in job.Documents.Where(d => d.Status == ProcessingStatus.Classified))
        {
            try
            {
                await ExtractDocumentAsync(document, blobStorage, documentIntelligence, job, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error extracting document {DocumentId}", document.Id);
                document.MarkAsFailed($"Extraction failed: {ex.Message}");
            }
        }

        // Validation phase
        job.StartValidation();
        processingJobRepository.Update(job);
        await processingJobRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var document in job.Documents.Where(d => d.Status == ProcessingStatus.Extracted))
        {
            ValidateDocument(document);
        }

        // Complete the job
        job.Complete();
        processingJobRepository.Update(job);
        await processingJobRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Completed processing job {JobId}", job.Id);
    }

    private async Task ClassifyDocumentAsync(
        ProcessedDocument document,
        IBlobStorageService blobStorage,
        IDocumentIntelligenceService documentIntelligence,
        ProcessingJob job,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Classifying document {DocumentId}: {FileName}", document.Id, document.OriginalFileName);

        // Download document from blob storage
        using var stream = await blobStorage.DownloadAsync(
            "email-attachments",
            GetBlobNameFromUrl(document.BlobStorageUrl),
            cancellationToken);

        // Classify document
        var result = await documentIntelligence.ClassifyDocumentAsync(
            stream, document.OriginalFileName, cancellationToken);

        job.OnDocumentClassified(document.Id, result.DocumentType, result.Confidence);

        logger.LogInformation("Classified document {DocumentId} as {DocumentType} with confidence {Confidence:P}",
            document.Id, result.DocumentType, result.Confidence);
    }

    private async Task ExtractDocumentAsync(
        ProcessedDocument document,
        IBlobStorageService blobStorage,
        IDocumentIntelligenceService documentIntelligence,
        ProcessingJob job,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Extracting document {DocumentId}: {DocumentType}", document.Id, document.DocumentType);

        // Download document from blob storage
        using var stream = await blobStorage.DownloadAsync(
            "email-attachments",
            GetBlobNameFromUrl(document.BlobStorageUrl),
            cancellationToken);

        // Extract based on document type
        switch (document.DocumentType)
        {
            case DocumentType.Acord125:
            case DocumentType.Acord126:
            case DocumentType.Acord130:
            case DocumentType.Acord140:
            case DocumentType.Acord127:
            case DocumentType.Acord137:
                await ExtractAcordFormAsync(document, stream, documentIntelligence, cancellationToken);
                break;

            case DocumentType.LossRunReport:
                await ExtractLossRunAsync(document, stream, documentIntelligence, cancellationToken);
                break;

            case DocumentType.ExposureSchedule:
                await ExtractExposureScheduleAsync(document, stream, documentIntelligence, cancellationToken);
                break;

            default:
                logger.LogWarning("No extractor available for document type {DocumentType}", document.DocumentType);
                break;
        }

        job.OnDocumentExtractionCompleted(document.Id);
    }

    private async Task ExtractAcordFormAsync(
        ProcessedDocument document,
        Stream stream,
        IDocumentIntelligenceService documentIntelligence,
        CancellationToken cancellationToken)
    {
        var result = await documentIntelligence.ExtractAcordFormAsync(
            stream, document.DocumentType, cancellationToken);

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
        IDocumentIntelligenceService documentIntelligence,
        CancellationToken cancellationToken)
    {
        var result = await documentIntelligence.ExtractLossRunAsync(stream, cancellationToken);

        // Convert loss run data to extracted fields
        for (var i = 0; i < result.Losses.Count; i++)
        {
            var loss = result.Losses[i];
            var prefix = $"Loss_{i + 1}_";

            AddFieldIfPresent(document, $"{prefix}DateOfLoss", loss.DateOfLoss?.ToString("yyyy-MM-dd"), 0.85m);
            AddFieldIfPresent(document, $"{prefix}ClaimNumber", loss.ClaimNumber, 0.90m);
            AddFieldIfPresent(document, $"{prefix}Description", loss.Description, 0.80m);
            AddFieldIfPresent(document, $"{prefix}PaidAmount", loss.PaidAmount?.ToString("F2"), 0.88m);
            AddFieldIfPresent(document, $"{prefix}ReservedAmount", loss.ReservedAmount?.ToString("F2"), 0.87m);
            AddFieldIfPresent(document, $"{prefix}Status", loss.Status, 0.85m);
        }

        document.CompleteExtraction();
    }

    private async Task ExtractExposureScheduleAsync(
        ProcessedDocument document,
        Stream stream,
        IDocumentIntelligenceService documentIntelligence,
        CancellationToken cancellationToken)
    {
        var result = await documentIntelligence.ExtractExposureScheduleAsync(stream, cancellationToken);

        // Convert location data to extracted fields
        for (var i = 0; i < result.Locations.Count; i++)
        {
            var location = result.Locations[i];
            var prefix = $"Location_{location.LocationNumber ?? (i + 1)}_";

            AddFieldIfPresent(document, $"{prefix}Street1", location.Street1, 0.90m);
            AddFieldIfPresent(document, $"{prefix}City", location.City, 0.92m);
            AddFieldIfPresent(document, $"{prefix}State", location.State, 0.95m);
            AddFieldIfPresent(document, $"{prefix}PostalCode", location.PostalCode, 0.88m);
            AddFieldIfPresent(document, $"{prefix}BuildingValue", location.BuildingValue?.ToString("F2"), 0.85m);
            AddFieldIfPresent(document, $"{prefix}ContentsValue", location.ContentsValue?.ToString("F2"), 0.84m);
            AddFieldIfPresent(document, $"{prefix}ConstructionType", location.ConstructionType, 0.80m);
            AddFieldIfPresent(document, $"{prefix}YearBuilt", location.YearBuilt?.ToString(), 0.82m);
        }

        document.CompleteExtraction();
    }

    private static void AddFieldIfPresent(ProcessedDocument document, string fieldName, string? value, decimal confidence)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var field = ExtractedField.Create(fieldName, value, confidence);
        if (field.IsSuccess)
        {
            document.AddExtractedField(field.Value);
        }
    }

    private void ValidateDocument(ProcessedDocument document)
    {
        // Check for required fields based on document type
        switch (document.DocumentType)
        {
            case DocumentType.Acord125:
                ValidateAcord125(document);
                break;

            case DocumentType.LossRunReport:
                if (!document.ExtractedFields.Any())
                {
                    document.AddValidationError("No loss data could be extracted from the loss run report.");
                }
                break;
        }

        document.CompleteValidation();
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
                return path[(containerIndex + "email-attachments/".Length)..];
            }
        }

        var uri = new Uri(url);
        var segments = uri.Segments;
        return string.Join("", segments.Skip(2)); // Skip container segment
    }
}
