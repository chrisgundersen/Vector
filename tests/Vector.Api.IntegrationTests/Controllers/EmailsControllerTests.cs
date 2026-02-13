using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vector.Api.Controllers.v1;
using Vector.Application.EmailIntake.Commands;
using Vector.Application.EmailIntake.DTOs;
using Vector.Application.EmailIntake.Queries;
using Vector.Domain.Common;

namespace Vector.Api.IntegrationTests.Controllers;

public class EmailsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<EmailsController>> _loggerMock;
    private readonly EmailsController _controller;

    private readonly Guid _tenantId = Guid.NewGuid();

    public EmailsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<EmailsController>>();
        _controller = new EmailsController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_WithExistingEmail_ReturnsOkWithEmail()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var emailDto = CreateEmailDto(emailId);

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetInboundEmailQuery>(q => q.EmailId == emailId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailDto);

        // Act
        var result = await _controller.GetById(emailId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().Be(emailDto);
    }

    [Fact]
    public async Task GetById_WithNonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetInboundEmailQuery>(q => q.EmailId == emailId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((InboundEmailDto?)null);

        // Act
        var result = await _controller.GetById(emailId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ProcessEmail_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new ProcessEmailRequest(
            _tenantId,
            "submissions@example.com",
            "msg-001",
            "broker@test.com",
            "Submission: ABC Manufacturing",
            "Please find attached...",
            "Full body content",
            DateTime.UtcNow);
        var newEmailId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
                It.Is<ProcessInboundEmailCommand>(c =>
                    c.TenantId == _tenantId &&
                    c.MailboxId == "submissions@example.com" &&
                    c.ExternalMessageId == "msg-001"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(newEmailId));

        // Act
        var result = await _controller.ProcessEmail(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(EmailsController.GetById));
        createdResult.Value.Should().Be(newEmailId);
    }

    [Fact]
    public async Task ProcessEmail_WithInvalidTenant_ReturnsBadRequest()
    {
        // Arrange
        var request = new ProcessEmailRequest(
            Guid.Empty,
            "submissions@example.com",
            "msg-001",
            "broker@test.com",
            "Submission: ABC Manufacturing",
            "Please find attached...",
            "Full body content",
            DateTime.UtcNow);

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<ProcessInboundEmailCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("InboundEmail.InvalidTenant", "Tenant ID is required.")));

        // Act
        var result = await _controller.ProcessEmail(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Tenant ID");
    }

    [Fact]
    public async Task ProcessEmail_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new ProcessEmailRequest(
            _tenantId,
            "submissions@example.com",
            "msg-duplicate",
            "broker@test.com",
            "Submission",
            "Preview",
            "Content",
            DateTime.UtcNow);

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<ProcessInboundEmailCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("InboundEmail.Duplicate", "Email already processed")));

        // Act
        var result = await _controller.ProcessEmail(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ExtractAttachment_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var content = "Test attachment content"u8.ToArray();
        var contentBase64 = Convert.ToBase64String(content);
        var request = new ExtractAttachmentRequest("ACORD125.pdf", "application/pdf", contentBase64);
        var attachmentId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
                It.Is<ExtractAttachmentCommand>(c =>
                    c.InboundEmailId == emailId &&
                    c.FileName == "ACORD125.pdf" &&
                    c.ContentType == "application/pdf"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(attachmentId));

        // Act
        var result = await _controller.ExtractAttachment(emailId, request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().Be(attachmentId);
        createdResult.Location.Should().Contain(emailId.ToString());
        createdResult.Location.Should().Contain(attachmentId.ToString());
    }

    [Fact]
    public async Task ExtractAttachment_WithNonExistentEmail_ReturnsBadRequest()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var content = "Test content"u8.ToArray();
        var contentBase64 = Convert.ToBase64String(content);
        var request = new ExtractAttachmentRequest("test.pdf", "application/pdf", contentBase64);

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<ExtractAttachmentCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("InboundEmail.NotFound", "Email not found")));

        // Act
        var result = await _controller.ExtractAttachment(emailId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ExtractAttachment_WithEmptyFileName_ReturnsBadRequest()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var content = "Test content"u8.ToArray();
        var contentBase64 = Convert.ToBase64String(content);
        var request = new ExtractAttachmentRequest("", "application/pdf", contentBase64);

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<ExtractAttachmentCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("Attachment.FileNameRequired", "File name is required")));

        // Act
        var result = await _controller.ExtractAttachment(emailId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    private InboundEmailDto CreateEmailDto(Guid id) =>
        new(
            Id: id,
            TenantId: _tenantId,
            ExternalMessageId: "msg-001",
            MailboxId: "submissions@example.com",
            FromAddress: "broker@test.com",
            Subject: "Submission: ABC Manufacturing",
            BodyPreview: "Please find attached...",
            ReceivedAt: DateTime.UtcNow.AddHours(-1),
            Status: "Received",
            AttachmentCount: 2,
            Attachments: Array.Empty<EmailAttachmentDto>());
}
