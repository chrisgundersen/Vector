using Vector.Domain.DocumentProcessing.Aggregates;
using Vector.Domain.DocumentProcessing.Enums;
using Vector.Domain.DocumentProcessing.ValueObjects;

namespace Vector.Domain.UnitTests.DocumentProcessing;

public class ProcessedDocumentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _inboundEmailId = Guid.NewGuid();

    private ProcessedDocument CreateDocument(string fileName = "ACORD125.pdf", string url = "https://storage/doc.pdf")
    {
        var job = ProcessingJob.Create(_tenantId, _inboundEmailId);
        return job.AddDocument(Guid.NewGuid(), fileName, url);
    }

    [Fact]
    public void Classify_SetsDocumentTypeAndConfidence()
    {
        // Arrange
        var document = CreateDocument();

        // Act
        document.Classify(DocumentType.Acord125, 0.95m);

        // Assert
        document.DocumentType.Should().Be(DocumentType.Acord125);
        document.ClassificationConfidence.Score.Should().Be(0.95m);
        document.Status.Should().Be(ProcessingStatus.Classified);
    }

    [Fact]
    public void Classify_WithInvalidConfidence_SetsUnknownConfidence()
    {
        // Arrange
        var document = CreateDocument();

        // Act
        document.Classify(DocumentType.Acord125, 1.5m);

        // Assert
        document.DocumentType.Should().Be(DocumentType.Acord125);
        document.ClassificationConfidence.Score.Should().Be(0m);
    }

    [Fact]
    public void AddExtractedField_AddsFieldToDocument()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        var fieldResult = ExtractedField.Create("InsuredName", "ABC Manufacturing", 0.98m);

        // Act
        document.AddExtractedField(fieldResult.Value);

        // Assert
        document.ExtractedFields.Should().HaveCount(1);
        document.ExtractedFields.First().FieldName.Should().Be("InsuredName");
        document.ExtractedFields.First().Value.Should().Be("ABC Manufacturing");
    }

    [Fact]
    public void AddExtractedField_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var document = CreateDocument();

        // Act & Assert
        var act = () => document.AddExtractedField(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddExtractedFields_AddsMultipleFields()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);

        var fields = new[]
        {
            ExtractedField.Create("InsuredName", "ABC Manufacturing", 0.98m).Value,
            ExtractedField.Create("EffectiveDate", "2024-04-01", 0.95m).Value,
            ExtractedField.Create("PolicyNumber", "GL-2024-001", 0.90m).Value
        };

        // Act
        document.AddExtractedFields(fields);

        // Assert
        document.ExtractedFields.Should().HaveCount(3);
    }

    [Fact]
    public void CompleteExtraction_SetsStatusToExtracted()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);

        // Act
        document.CompleteExtraction();

        // Assert
        document.Status.Should().Be(ProcessingStatus.Extracted);
    }

    [Fact]
    public void AddValidationError_AddsErrorToList()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.CompleteExtraction();

        // Act
        document.AddValidationError("Missing required field: Insured Name");

        // Assert
        document.ValidationErrors.Should().HaveCount(1);
        document.ValidationErrors.Should().Contain("Missing required field: Insured Name");
    }

    [Fact]
    public void AddValidationError_WithEmptyString_DoesNotAdd()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.CompleteExtraction();

        // Act
        document.AddValidationError("");
        document.AddValidationError("   ");

        // Assert
        document.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public void CompleteValidation_WithNoErrors_SetsStatusToCompleted()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.CompleteExtraction();

        // Act
        document.CompleteValidation();

        // Assert
        document.Status.Should().Be(ProcessingStatus.Completed);
        document.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CompleteValidation_WithErrors_SetsStatusToManualReviewRequired()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.CompleteExtraction();
        document.AddValidationError("Missing required field: Insured Name");

        // Act
        document.CompleteValidation();

        // Assert
        document.Status.Should().Be(ProcessingStatus.ManualReviewRequired);
    }

    [Fact]
    public void MarkAsFailed_SetsStatusToFailedWithReason()
    {
        // Arrange
        var document = CreateDocument();

        // Act
        document.MarkAsFailed("OCR extraction failed");

        // Assert
        document.Status.Should().Be(ProcessingStatus.Failed);
        document.FailureReason.Should().Be("OCR extraction failed");
        document.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetAverageConfidence_ReturnsAverageOfFieldConfidences()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.AddExtractedField(ExtractedField.Create("Field1", "Value1", 0.90m).Value);
        document.AddExtractedField(ExtractedField.Create("Field2", "Value2", 0.80m).Value);
        document.AddExtractedField(ExtractedField.Create("Field3", "Value3", 1.00m).Value);

        // Act
        var avgConfidence = document.GetAverageConfidence();

        // Assert
        avgConfidence.Should().Be(0.90m);
    }

    [Fact]
    public void GetAverageConfidence_WithNoFields_ReturnsZero()
    {
        // Arrange
        var document = CreateDocument();

        // Act
        var avgConfidence = document.GetAverageConfidence();

        // Assert
        avgConfidence.Should().Be(0);
    }

    [Fact]
    public void GetField_ReturnsFieldByName()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.AddExtractedField(ExtractedField.Create("InsuredName", "ABC Manufacturing", 0.98m).Value);

        // Act
        var field = document.GetField("InsuredName");

        // Assert
        field.Should().NotBeNull();
        field!.Value.Should().Be("ABC Manufacturing");
    }

    [Fact]
    public void GetField_IsCaseInsensitive()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.AddExtractedField(ExtractedField.Create("InsuredName", "ABC Manufacturing", 0.98m).Value);

        // Act
        var field = document.GetField("insuredname");

        // Assert
        field.Should().NotBeNull();
        field!.Value.Should().Be("ABC Manufacturing");
    }

    [Fact]
    public void GetField_WithNonExistentName_ReturnsNull()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.AddExtractedField(ExtractedField.Create("InsuredName", "ABC Manufacturing", 0.98m).Value);

        // Act
        var field = document.GetField("NonExistent");

        // Assert
        field.Should().BeNull();
    }

    [Fact]
    public void GetFieldValue_ReturnsValueDirectly()
    {
        // Arrange
        var document = CreateDocument();
        document.Classify(DocumentType.Acord125, 0.95m);
        document.AddExtractedField(ExtractedField.Create("InsuredName", "ABC Manufacturing", 0.98m).Value);

        // Act
        var value = document.GetFieldValue("InsuredName");

        // Assert
        value.Should().Be("ABC Manufacturing");
    }

    [Fact]
    public void GetFieldValue_WithNonExistentName_ReturnsNull()
    {
        // Arrange
        var document = CreateDocument();

        // Act
        var value = document.GetFieldValue("NonExistent");

        // Assert
        value.Should().BeNull();
    }
}
