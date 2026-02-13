using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.DocumentProcessing.Events;

namespace Vector.Domain.UnitTests.DocumentProcessing;

public class ProcessingJobTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _inboundEmailId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesProcessingJob()
    {
        // Act
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);

        // Assert
        job.TenantId.Should().Be(_tenantId);
        job.InboundEmailId.Should().Be(_inboundEmailId);
        job.Status.Should().Be(ProcessingStatus.Pending);
        job.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.CompletedAt.Should().BeNull();
        job.ErrorMessage.Should().BeNull();
        job.Documents.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => ProcessingJob.Create(Guid.Empty, _inboundEmailId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tenant ID*");
    }

    [Fact]
    public void Create_WithEmptyInboundEmailId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => ProcessingJob.Create(_tenantId, Guid.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Inbound email ID*");
    }

    [Fact]
    public void AddDocument_AddsDocumentToJob()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var attachmentId = Guid.NewGuid();

        // Act
        var document = job.AddDocument(attachmentId, "ACORD125.pdf", "https://storage/acord.pdf");

        // Assert
        job.Documents.Should().HaveCount(1);
        document.SourceAttachmentId.Should().Be(attachmentId);
        document.OriginalFileName.Should().Be("ACORD125.pdf");
        document.BlobStorageUrl.Should().Be("https://storage/acord.pdf");
        document.Status.Should().Be(ProcessingStatus.Pending);
        document.DocumentType.Should().Be(DocumentType.Unknown);
    }

    [Fact]
    public void StartClassification_SetsStatusToClassifying()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);

        // Act
        job.StartClassification();

        // Assert
        job.Status.Should().Be(ProcessingStatus.Classifying);
    }

    [Fact]
    public void OnDocumentClassified_ClassifiesDocumentAndRaisesEvent()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var document = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.StartClassification();

        // Act
        job.OnDocumentClassified(document.Id, DocumentType.Acord125, 0.95m);

        // Assert
        document.DocumentType.Should().Be(DocumentType.Acord125);
        document.Status.Should().Be(ProcessingStatus.Classified);
        job.DomainEvents.Should().ContainSingle(e => e is DocumentClassifiedEvent);
    }

    [Fact]
    public void OnDocumentClassified_WithNonExistentDocument_DoesNothing()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.StartClassification();

        // Act
        job.OnDocumentClassified(Guid.NewGuid(), DocumentType.Acord125, 0.95m);

        // Assert
        job.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void StartExtraction_SetsStatusToExtracting()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        job.StartClassification();

        // Act
        job.StartExtraction();

        // Assert
        job.Status.Should().Be(ProcessingStatus.Extracting);
    }

    [Fact]
    public void OnDocumentExtractionCompleted_CompletesExtractionAndRaisesEvent()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var document = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(document.Id, DocumentType.Acord125, 0.95m);
        job.StartExtraction();

        // Act
        job.OnDocumentExtractionCompleted(document.Id);

        // Assert
        document.Status.Should().Be(ProcessingStatus.Extracted);
        job.DomainEvents.Should().Contain(e => e is DocumentExtractionCompletedEvent);
    }

    [Fact]
    public void StartValidation_SetsStatusToValidating()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        job.StartExtraction();

        // Act
        job.StartValidation();

        // Assert
        job.Status.Should().Be(ProcessingStatus.Validating);
    }

    [Fact]
    public void Complete_SetsStatusToCompletedAndRaisesEvent()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var document = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(document.Id, DocumentType.Acord125, 0.95m);
        job.OnDocumentExtractionCompleted(document.Id);
        job.StartValidation();
        job.ClearDomainEvents();

        // Act
        job.Complete();

        // Assert
        job.Status.Should().Be(ProcessingStatus.Completed);
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.DomainEvents.Should().ContainSingle(e => e is ProcessingJobCompletedEvent);
    }

    [Fact]
    public void Fail_SetsStatusToFailedWithErrorMessage()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);

        // Act
        job.Fail("Document parsing failed");

        // Assert
        job.Status.Should().Be(ProcessingStatus.Failed);
        job.ErrorMessage.Should().Be("Document parsing failed");
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetDocument_ReturnsDocumentById()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var document = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");

        // Act
        var result = job.GetDocument(document.Id);

        // Assert
        result.Should().Be(document);
    }

    [Fact]
    public void GetDocument_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");

        // Act
        var result = job.GetDocument(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDocumentsByType_ReturnsMatchingDocuments()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc1 = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord125.pdf");
        var doc2 = job.AddDocument(Guid.NewGuid(), "ACORD126.pdf", "https://storage/acord126.pdf");
        var doc3 = job.AddDocument(Guid.NewGuid(), "LossRuns.pdf", "https://storage/lossruns.pdf");

        job.OnDocumentClassified(doc1.Id, DocumentType.Acord125, 0.95m);
        job.OnDocumentClassified(doc2.Id, DocumentType.Acord125, 0.90m);
        job.OnDocumentClassified(doc3.Id, DocumentType.LossRunReport, 0.85m);

        // Act
        var result = job.GetDocumentsByType(DocumentType.Acord125).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(doc1);
        result.Should().Contain(doc2);
    }

    [Fact]
    public void HasAcordForms_WithAcordDocuments_ReturnsTrue()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var document = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(document.Id, DocumentType.Acord125, 0.95m);

        // Act & Assert
        job.HasAcordForms.Should().BeTrue();
    }

    [Fact]
    public void HasAcordForms_WithoutAcordDocuments_ReturnsFalse()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var document = job.AddDocument(Guid.NewGuid(), "LossRuns.pdf", "https://storage/lossruns.pdf");
        job.OnDocumentClassified(document.Id, DocumentType.LossRunReport, 0.85m);

        // Act & Assert
        job.HasAcordForms.Should().BeFalse();
    }

    [Fact]
    public void HasLossRuns_WithLossRunDocument_ReturnsTrue()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var document = job.AddDocument(Guid.NewGuid(), "LossRuns.pdf", "https://storage/lossruns.pdf");
        job.OnDocumentClassified(document.Id, DocumentType.LossRunReport, 0.85m);

        // Act & Assert
        job.HasLossRuns.Should().BeTrue();
    }

    [Fact]
    public void HasExposureSchedule_WithExposureDocument_ReturnsTrue()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var document = job.AddDocument(Guid.NewGuid(), "SOV.xlsx", "https://storage/sov.xlsx");
        job.OnDocumentClassified(document.Id, DocumentType.ExposureSchedule, 0.90m);

        // Act & Assert
        job.HasExposureSchedule.Should().BeTrue();
    }
}
