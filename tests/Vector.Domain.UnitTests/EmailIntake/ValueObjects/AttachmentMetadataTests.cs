using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Domain.UnitTests.EmailIntake.ValueObjects;

public class AttachmentMetadataTests
{
    private static ContentHash CreateHash() => ContentHash.ComputeSha256("test content");

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var hash = CreateHash();

        // Act
        var result = AttachmentMetadata.Create("document.pdf", "application/pdf", 1024, hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("document.pdf");
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.SizeInBytes.Should().Be(1024);
        result.Value.ContentHash.Should().Be(hash);
    }

    [Fact]
    public void Create_WithZeroSize_ReturnsSuccess()
    {
        // Arrange
        var hash = CreateHash();

        // Act
        var result = AttachmentMetadata.Create("empty.txt", "text/plain", 0, hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SizeInBytes.Should().Be(0);
    }

    [Fact]
    public void Create_WithEmptyFileName_ReturnsFailure()
    {
        // Arrange
        var hash = CreateHash();

        // Act
        var result = AttachmentMetadata.Create("", "application/pdf", 1024, hash);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.FileNameEmpty");
    }

    [Fact]
    public void Create_WithWhitespaceFileName_ReturnsFailure()
    {
        // Arrange
        var hash = CreateHash();

        // Act
        var result = AttachmentMetadata.Create("   ", "application/pdf", 1024, hash);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.FileNameEmpty");
    }

    [Fact]
    public void Create_WithTooLongFileName_ReturnsFailure()
    {
        // Arrange
        var hash = CreateHash();
        var longFileName = new string('a', 256) + ".pdf";

        // Act
        var result = AttachmentMetadata.Create(longFileName, "application/pdf", 1024, hash);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.FileNameTooLong");
    }

    [Fact]
    public void Create_WithMaxLengthFileName_ReturnsSuccess()
    {
        // Arrange
        var hash = CreateHash();
        var maxFileName = new string('a', 251) + ".pdf"; // 255 characters total

        // Act
        var result = AttachmentMetadata.Create(maxFileName, "application/pdf", 1024, hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyContentType_ReturnsFailure()
    {
        // Arrange
        var hash = CreateHash();

        // Act
        var result = AttachmentMetadata.Create("document.pdf", "", 1024, hash);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.ContentTypeEmpty");
    }

    [Fact]
    public void Create_WithWhitespaceContentType_ReturnsFailure()
    {
        // Arrange
        var hash = CreateHash();

        // Act
        var result = AttachmentMetadata.Create("document.pdf", "   ", 1024, hash);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.ContentTypeEmpty");
    }

    [Fact]
    public void Create_WithNegativeSize_ReturnsFailure()
    {
        // Arrange
        var hash = CreateHash();

        // Act
        var result = AttachmentMetadata.Create("document.pdf", "application/pdf", -1, hash);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.InvalidSize");
    }

    [Fact]
    public void FileExtension_ReturnLowercaseExtension()
    {
        // Arrange
        var hash = CreateHash();
        var metadata = AttachmentMetadata.Create("Document.PDF", "application/pdf", 1024, hash).Value;

        // Act
        var extension = metadata.FileExtension;

        // Assert
        extension.Should().Be(".pdf");
    }

    [Fact]
    public void FileExtension_WithNoExtension_ReturnsEmptyString()
    {
        // Arrange
        var hash = CreateHash();
        var metadata = AttachmentMetadata.Create("README", "text/plain", 1024, hash).Value;

        // Act
        var extension = metadata.FileExtension;

        // Assert
        extension.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf", true)]
    [InlineData("document.PDF", "application/octet-stream", true)]
    [InlineData("document.txt", "application/pdf", true)]
    [InlineData("document.txt", "text/plain", false)]
    public void IsPdf_ReturnsCorrectValue(string fileName, string contentType, bool expectedIsPdf)
    {
        // Arrange
        var hash = CreateHash();
        var metadata = AttachmentMetadata.Create(fileName, contentType, 1024, hash).Value;

        // Act
        var isPdf = metadata.IsPdf;

        // Assert
        isPdf.Should().Be(expectedIsPdf);
    }

    [Theory]
    [InlineData("image.jpg", "image/jpeg", true)]
    [InlineData("image.png", "image/png", true)]
    [InlineData("image.gif", "image/gif", true)]
    [InlineData("image.webp", "IMAGE/WEBP", true)]
    [InlineData("document.pdf", "application/pdf", false)]
    [InlineData("image.txt", "text/plain", false)]
    public void IsImage_ReturnsCorrectValue(string fileName, string contentType, bool expectedIsImage)
    {
        // Arrange
        var hash = CreateHash();
        var metadata = AttachmentMetadata.Create(fileName, contentType, 1024, hash).Value;

        // Act
        var isImage = metadata.IsImage;

        // Assert
        isImage.Should().Be(expectedIsImage);
    }

    [Theory]
    [InlineData("spreadsheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", true)]
    [InlineData("spreadsheet.xls", "application/vnd.ms-excel", true)]
    [InlineData("spreadsheet.xlsx", "application/octet-stream", true)]
    [InlineData("spreadsheet.xls", "application/octet-stream", true)]
    [InlineData("document.pdf", "application/pdf", false)]
    [InlineData("document.csv", "text/csv", false)]
    public void IsExcel_ReturnsCorrectValue(string fileName, string contentType, bool expectedIsExcel)
    {
        // Arrange
        var hash = CreateHash();
        var metadata = AttachmentMetadata.Create(fileName, contentType, 1024, hash).Value;

        // Act
        var isExcel = metadata.IsExcel;

        // Assert
        isExcel.Should().Be(expectedIsExcel);
    }

    [Fact]
    public void Equality_WithSameData_AreEqual()
    {
        // Arrange
        var hash = CreateHash();
        var metadata1 = AttachmentMetadata.Create("document.pdf", "application/pdf", 1024, hash).Value;
        var metadata2 = AttachmentMetadata.Create("document.pdf", "application/pdf", 1024, hash).Value;

        // Assert
        metadata1.Should().Be(metadata2);
    }

    [Fact]
    public void Equality_WithDifferentFileName_AreNotEqual()
    {
        // Arrange
        var hash = CreateHash();
        var metadata1 = AttachmentMetadata.Create("document1.pdf", "application/pdf", 1024, hash).Value;
        var metadata2 = AttachmentMetadata.Create("document2.pdf", "application/pdf", 1024, hash).Value;

        // Assert
        metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void Equality_WithDifferentContentType_AreNotEqual()
    {
        // Arrange
        var hash = CreateHash();
        var metadata1 = AttachmentMetadata.Create("document.pdf", "application/pdf", 1024, hash).Value;
        var metadata2 = AttachmentMetadata.Create("document.pdf", "text/plain", 1024, hash).Value;

        // Assert
        metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void Equality_WithDifferentSize_AreNotEqual()
    {
        // Arrange
        var hash = CreateHash();
        var metadata1 = AttachmentMetadata.Create("document.pdf", "application/pdf", 1024, hash).Value;
        var metadata2 = AttachmentMetadata.Create("document.pdf", "application/pdf", 2048, hash).Value;

        // Assert
        metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void Equality_WithDifferentHash_AreNotEqual()
    {
        // Arrange
        var hash1 = ContentHash.ComputeSha256("content 1");
        var hash2 = ContentHash.ComputeSha256("content 2");
        var metadata1 = AttachmentMetadata.Create("document.pdf", "application/pdf", 1024, hash1).Value;
        var metadata2 = AttachmentMetadata.Create("document.pdf", "application/pdf", 1024, hash2).Value;

        // Assert
        metadata1.Should().NotBe(metadata2);
    }
}
