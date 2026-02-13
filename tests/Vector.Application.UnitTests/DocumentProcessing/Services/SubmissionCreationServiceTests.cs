using Microsoft.Extensions.Logging;
using Vector.Application.DocumentProcessing.Services;
using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.DocumentProcessing.ValueObjects;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.UnitTests.DocumentProcessing.Services;

public class SubmissionCreationServiceTests
{
    private readonly Mock<ILogger<SubmissionCreationService>> _loggerMock;
    private readonly SubmissionCreationService _service;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _inboundEmailId = Guid.NewGuid();

    public SubmissionCreationServiceTests()
    {
        _loggerMock = new Mock<ILogger<SubmissionCreationService>>();
        _service = new SubmissionCreationService(_loggerMock.Object);
    }

    #region CreateSubmissionFromJobAsync Tests

    [Fact]
    public async Task CreateSubmissionFromJobAsync_WithCompletedJob_ReturnsSubmission()
    {
        // Arrange
        var job = CreateCompletedJobWithAcordForm();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.ProcessingJobId.Should().Be(job.Id);
        result.Value.InboundEmailId.Should().Be(_inboundEmailId);
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_WithIncompleteJob_ReturnsFailure()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        AddAcordDocumentWithFields(job);
        // Job is not completed (status is still Pending)

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SubmissionCreation.ProcessingJobNotCompleted");
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_WithNoAcordForm_ReturnsFailure()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "LossRun.pdf", "https://storage/lossrun.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.LossRunReport, 0.90m);
        job.OnDocumentExtractionCompleted(doc.Id);
        job.StartValidation();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SubmissionCreation.NoAcordFormFound");
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_WithNoInsuredName_ReturnsFailure()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord125, 0.95m);
        // Don't add InsuredName field
        doc.AddExtractedField(ExtractedField.Create("EffectiveDate", "2024-04-01", 0.90m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SubmissionCreation.InsuredNameNotExtracted");
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_ExtractsInsuredName_SetsOnSubmission()
    {
        // Arrange
        var job = CreateCompletedJobWithAcordForm();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Insured.Name.Should().Be("ABC Manufacturing Corp");
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_ExtractsInsuredAddress_SetsOnSubmission()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord125, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredAddress", "456 Oak Avenue", 0.92m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredCity", "New York", 0.94m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredState", "NY", 0.97m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredZip", "10001", 0.96m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Insured.MailingAddress.Should().NotBeNull();
        result.Value.Insured.MailingAddress!.Street1.Should().Be("456 Oak Avenue");
        result.Value.Insured.MailingAddress.City.Should().Be("New York");
        result.Value.Insured.MailingAddress.State.Should().Be("NY");
        result.Value.Insured.MailingAddress.PostalCode.Should().Be("10001");
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_ExtractsDBA_SetsOnSubmission()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord125, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredDBA", "Test Trading Co", 0.88m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.Value.Insured.DbaName.Should().Be("Test Trading Co");
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_ExtractsFEIN_SetsOnSubmission()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord125, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("FEIN", "12-3456789", 0.90m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.Value.Insured.FeinNumber.Should().Be("123456789");
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_ExtractsEffectiveDate_SetsOnSubmission()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord125, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("EffectiveDate", "2024-06-01", 0.92m).Value);
        doc.AddExtractedField(ExtractedField.Create("ExpirationDate", "2025-06-01", 0.91m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.Value.EffectiveDate.Should().Be(new DateTime(2024, 6, 1));
        result.Value.ExpirationDate.Should().Be(new DateTime(2025, 6, 1));
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_WithAcord126_AddsCoverages()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD126.pdf", "https://storage/acord126.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord126, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("GLLimit", "1000000", 0.90m).Value);
        doc.AddExtractedField(ExtractedField.Create("GLDeductible", "10000", 0.88m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.Value.Coverages.Should().HaveCount(1);
        result.Value.Coverages.First().Type.Should().Be(CoverageType.GeneralLiability);
        result.Value.Coverages.First().RequestedLimit!.Amount.Should().Be(1000000m);
        result.Value.Coverages.First().RequestedDeductible!.Amount.Should().Be(10000m);
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_WithAcord130_AddsWorkersCompCoverage()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD130.pdf", "https://storage/acord130.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord130, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("WCLimit", "500000", 0.90m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.Value.Coverages.Should().ContainSingle(c => c.Type == CoverageType.WorkersCompensation);
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_WithAcord140_AddsPropertyCoverage()
    {
        // Arrange
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD140.pdf", "https://storage/acord140.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord140, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("PropertyLimit", "2000000", 0.90m).Value);
        doc.AddExtractedField(ExtractedField.Create("PropertyDeductible", "25000", 0.88m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.Value.Coverages.Should().ContainSingle(c => c.Type == CoverageType.PropertyDamage);
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_SubmissionIsMarkedAsReceived()
    {
        // Arrange
        var job = CreateCompletedJobWithAcordForm();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.Value.Status.Should().Be(SubmissionStatus.Received);
    }

    [Fact]
    public async Task CreateSubmissionFromJobAsync_GeneratesSubmissionNumber()
    {
        // Arrange
        var job = CreateCompletedJobWithAcordForm();

        // Act
        var result = await _service.CreateSubmissionFromJobAsync(job);

        // Assert
        result.Value.SubmissionNumber.Should().StartWith("SUB-");
        result.Value.SubmissionNumber.Should().MatchRegex(@"SUB-\d{8}-\d{4}");
    }

    #endregion

    #region EnrichSubmissionAsync Tests

    [Fact]
    public async Task EnrichSubmissionAsync_WithCompletedJob_EnrichesSubmission()
    {
        // Arrange
        var submissionResult = Submission.Create(_tenantId, "SUB-TEST-001", "Initial Name", null, null);
        var submission = submissionResult.Value;

        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord125, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "Enriched Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("YearsInBusiness", "10", 0.88m).Value);
        doc.AddExtractedField(ExtractedField.Create("EmployeeCount", "150", 0.90m).Value);
        doc.CompleteExtraction();
        job.Complete();

        // Act
        var result = await _service.EnrichSubmissionAsync(submission, job);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Insured.YearsInBusiness.Should().Be(10);
        submission.Insured.EmployeeCount.Should().Be(150);
    }

    [Fact]
    public async Task EnrichSubmissionAsync_WithIncompleteJob_ReturnsFailure()
    {
        // Arrange
        var submissionResult = Submission.Create(_tenantId, "SUB-TEST-001", "Test Corp", null, null);
        var submission = submissionResult.Value;

        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        // Job not completed

        // Act
        var result = await _service.EnrichSubmissionAsync(submission, job);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SubmissionCreation.ProcessingJobNotCompleted");
    }

    [Fact]
    public async Task EnrichSubmissionAsync_WithLossRuns_AddsLossHistory()
    {
        // Arrange
        var submissionResult = Submission.Create(_tenantId, "SUB-TEST-001", "Test Corp", null, null);
        var submission = submissionResult.Value;

        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);

        var acordDoc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(acordDoc.Id, DocumentType.Acord125, 0.95m);
        acordDoc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        acordDoc.CompleteExtraction();

        var lossDoc = job.AddDocument(Guid.NewGuid(), "LossRun.pdf", "https://storage/lossrun.pdf");
        job.OnDocumentClassified(lossDoc.Id, DocumentType.LossRunReport, 0.90m);
        lossDoc.AddExtractedField(ExtractedField.Create("LossCount", "2", 0.95m).Value);
        lossDoc.AddExtractedField(ExtractedField.Create("CarrierName", "ABC Insurance", 0.92m).Value);
        lossDoc.AddExtractedField(ExtractedField.Create("Loss_1_DateOfLoss", "2023-01-15", 0.90m).Value);
        lossDoc.AddExtractedField(ExtractedField.Create("Loss_1_Description", "Water damage", 0.88m).Value);
        lossDoc.AddExtractedField(ExtractedField.Create("Loss_1_PaidAmount", "50000", 0.91m).Value);
        lossDoc.AddExtractedField(ExtractedField.Create("Loss_1_ClaimNumber", "CLM-001", 0.89m).Value);
        lossDoc.AddExtractedField(ExtractedField.Create("Loss_2_DateOfLoss", "2023-06-20", 0.90m).Value);
        lossDoc.AddExtractedField(ExtractedField.Create("Loss_2_Description", "Fire damage", 0.88m).Value);
        lossDoc.AddExtractedField(ExtractedField.Create("Loss_2_PaidAmount", "100000", 0.91m).Value);
        lossDoc.CompleteExtraction();

        job.Complete();

        // Act
        var result = await _service.EnrichSubmissionAsync(submission, job);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.LossHistory.Should().HaveCount(2);

        var firstLoss = submission.LossHistory.First();
        firstLoss.DateOfLoss.Should().Be(new DateTime(2023, 1, 15));
        firstLoss.Description.Should().Be("Water damage");
        firstLoss.PaidAmount!.Amount.Should().Be(50000m);
        firstLoss.ClaimNumber.Should().Be("CLM-001");
    }

    [Fact]
    public async Task EnrichSubmissionAsync_WithExposureSchedule_AddsLocations()
    {
        // Arrange
        var submissionResult = Submission.Create(_tenantId, "SUB-TEST-001", "Test Corp", null, null);
        var submission = submissionResult.Value;

        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);

        var acordDoc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(acordDoc.Id, DocumentType.Acord125, 0.95m);
        acordDoc.AddExtractedField(ExtractedField.Create("InsuredName", "Test Corp", 0.95m).Value);
        acordDoc.CompleteExtraction();

        var exposureDoc = job.AddDocument(Guid.NewGuid(), "SOV.xlsx", "https://storage/sov.xlsx");
        job.OnDocumentClassified(exposureDoc.Id, DocumentType.ExposureSchedule, 0.90m);
        exposureDoc.AddExtractedField(ExtractedField.Create("LocationCount", "2", 0.95m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_1_Street1", "100 Industrial Pkwy", 0.92m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_1_City", "Chicago", 0.94m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_1_State", "IL", 0.97m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_1_PostalCode", "60601", 0.96m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_1_BuildingValue", "5000000", 0.90m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_1_ContentsValue", "1000000", 0.89m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_1_ConstructionType", "Masonry", 0.88m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_1_YearBuilt", "1990", 0.85m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_2_Street1", "200 Commerce Dr", 0.92m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_2_City", "Aurora", 0.94m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_2_State", "IL", 0.97m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_2_PostalCode", "60505", 0.96m).Value);
        exposureDoc.AddExtractedField(ExtractedField.Create("Location_2_BuildingValue", "3000000", 0.90m).Value);
        exposureDoc.CompleteExtraction();

        job.Complete();

        // Act
        var result = await _service.EnrichSubmissionAsync(submission, job);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.Locations.Should().HaveCount(2);

        var firstLocation = submission.Locations.First();
        firstLocation.Address.Street1.Should().Be("100 Industrial Pkwy");
        firstLocation.Address.City.Should().Be("Chicago");
        firstLocation.BuildingValue!.Amount.Should().Be(5000000m);
        firstLocation.ContentsValue!.Amount.Should().Be(1000000m);
        firstLocation.ConstructionType.Should().Be("Masonry");
        firstLocation.YearBuilt.Should().Be(1990);
    }

    #endregion

    #region Helper Methods

    private ProcessingJob CreateCompletedJobWithAcordForm()
    {
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        AddAcordDocumentWithFields(job);
        job.Complete();
        return job;
    }

    private void AddAcordDocumentWithFields(ProcessingJob job)
    {
        var doc = job.AddDocument(Guid.NewGuid(), "ACORD125.pdf", "https://storage/acord.pdf");
        job.OnDocumentClassified(doc.Id, DocumentType.Acord125, 0.95m);

        doc.AddExtractedField(ExtractedField.Create("InsuredName", "ABC Manufacturing Corp", 0.95m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredAddress", "123 Main St", 0.92m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredCity", "Chicago", 0.94m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredState", "IL", 0.97m).Value);
        doc.AddExtractedField(ExtractedField.Create("InsuredZip", "60601", 0.96m).Value);
        doc.AddExtractedField(ExtractedField.Create("EffectiveDate", "2024-04-01", 0.90m).Value);
        doc.CompleteExtraction();

        job.OnDocumentExtractionCompleted(doc.Id);
    }

    #endregion
}
