using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Application.EmailIntake.Commands;
using Vector.Domain.Common;
using Vector.Domain.EmailIntake;
using Vector.Domain.EmailIntake.Aggregates;

namespace Vector.Application.UnitTests.EmailIntake.Commands;

public class ExtractAttachmentCommandHandlerTests
{
    private readonly Mock<IInboundEmailRepository> _emailRepositoryMock;
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
    private readonly Mock<ILogger<ExtractAttachmentCommandHandler>> _loggerMock;
    private readonly ExtractAttachmentCommandHandler _handler;

    public ExtractAttachmentCommandHandlerTests()
    {
        _emailRepositoryMock = new Mock<IInboundEmailRepository>();
        _blobStorageServiceMock = new Mock<IBlobStorageService>();
        _loggerMock = new Mock<ILogger<ExtractAttachmentCommandHandler>>();

        _handler = new ExtractAttachmentCommandHandler(
            _emailRepositoryMock.Object,
            _blobStorageServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidAttachment_ReturnsSuccessWithAttachmentId()
    {
        // Arrange
        var email = CreateTestEmail();
        var command = new ExtractAttachmentCommand(
            email.Id,
            "test-document.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3, 4, 5 });

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        _blobStorageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/blob/test.pdf");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _blobStorageServiceMock.Verify(x => x.UploadAsync(
            "email-attachments",
            It.Is<string>(s => s.Contains(email.TenantId.ToString())),
            It.IsAny<Stream>(),
            "application/pdf",
            It.IsAny<CancellationToken>()), Times.Once);

        _emailRepositoryMock.Verify(x => x.Update(email), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsFailure()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var command = new ExtractAttachmentCommand(
            emailId,
            "test-document.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3, 4, 5 });

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InboundEmail?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.NotFound");

        _blobStorageServiceMock.Verify(x => x.UploadAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyFileName_ReturnsFailure()
    {
        // Arrange
        var email = CreateTestEmail();
        var command = new ExtractAttachmentCommand(
            email.Id,
            "",
            "application/pdf",
            new byte[] { 1, 2, 3, 4, 5 });

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        _blobStorageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/blob/test.pdf");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.FileNameEmpty");

        _blobStorageServiceMock.Verify(x => x.DeleteAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyContentType_ReturnsFailure()
    {
        // Arrange
        var email = CreateTestEmail();
        var command = new ExtractAttachmentCommand(
            email.Id,
            "test-document.pdf",
            "",
            new byte[] { 1, 2, 3, 4, 5 });

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        _blobStorageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/blob/test.pdf");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AttachmentMetadata.ContentTypeEmpty");
    }

    [Fact]
    public async Task Handle_WithLargeAttachment_UploadsSuccessfully()
    {
        // Arrange
        var email = CreateTestEmail();
        var largeContent = new byte[10 * 1024 * 1024]; // 10 MB
        new Random().NextBytes(largeContent);

        var command = new ExtractAttachmentCommand(
            email.Id,
            "large-document.pdf",
            "application/pdf",
            largeContent);

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        _blobStorageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/blob/large.pdf");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithMultipleAttachments_AddsAllToEmail()
    {
        // Arrange
        var email = CreateTestEmail();

        _emailRepositoryMock.Setup(x => x.GetByIdAsync(email.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        _blobStorageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/blob/test.pdf");

        // Act - Add first attachment
        var result1 = await _handler.Handle(new ExtractAttachmentCommand(
            email.Id, "doc1.pdf", "application/pdf", new byte[] { 1, 2, 3 }), CancellationToken.None);

        // Add second attachment
        var result2 = await _handler.Handle(new ExtractAttachmentCommand(
            email.Id, "doc2.pdf", "application/pdf", new byte[] { 4, 5, 6 }), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        email.Attachments.Should().HaveCount(2);
    }

    private static InboundEmail CreateTestEmail()
    {
        var result = InboundEmail.Create(
            Guid.NewGuid(),
            "msg-123",
            "inbox@test.com",
            "sender@example.com",
            "Test Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        return result.Value;
    }
}
