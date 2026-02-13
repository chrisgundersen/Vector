using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vector.Api.Controllers.v1;
using Vector.Application.Submissions.Commands;
using Vector.Application.Submissions.DTOs;
using Vector.Application.Submissions.Queries;
using Vector.Domain.Common;

namespace Vector.Api.IntegrationTests.Controllers;

public class SubmissionsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<SubmissionsController>> _loggerMock;
    private readonly SubmissionsController _controller;

    private readonly Guid _tenantId = Guid.NewGuid();

    public SubmissionsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SubmissionsController>>();
        _controller = new SubmissionsController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_WithExistingSubmission_ReturnsOkWithSubmission()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var submissionDto = CreateSubmissionDto(submissionId);

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetSubmissionQuery>(q => q.SubmissionId == submissionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissionDto);

        // Act
        var result = await _controller.GetById(submissionId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().Be(submissionDto);
    }

    [Fact]
    public async Task GetById_WithNonExistentSubmission_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetSubmissionQuery>(q => q.SubmissionId == submissionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubmissionDto?)null);

        // Act
        var result = await _controller.GetById(submissionId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateSubmissionRequest(_tenantId, "ABC Manufacturing");
        var newSubmissionId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
                It.Is<CreateSubmissionCommand>(c =>
                    c.TenantId == _tenantId &&
                    c.InsuredName == "ABC Manufacturing"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(newSubmissionId));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(SubmissionsController.GetById));
        createdResult.Value.Should().Be(newSubmissionId);
        createdResult.RouteValues.Should().ContainKey("id");
        createdResult.RouteValues!["id"].Should().Be(newSubmissionId);
    }

    [Fact]
    public async Task Create_WithProcessingJobId_PassesIdToCommand()
    {
        // Arrange
        var processingJobId = Guid.NewGuid();
        var request = new CreateSubmissionRequest(_tenantId, "ABC Manufacturing", processingJobId);
        var newSubmissionId = Guid.NewGuid();

        CreateSubmissionCommand? capturedCommand = null;
        _mediatorMock.Setup(m => m.Send(
                It.IsAny<CreateSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<Guid>>, CancellationToken>((cmd, _) =>
                capturedCommand = (CreateSubmissionCommand)cmd)
            .ReturnsAsync(Result.Success(newSubmissionId));

        // Act
        await _controller.Create(request, CancellationToken.None);

        // Assert
        capturedCommand.Should().NotBeNull();
        capturedCommand!.ProcessingJobId.Should().Be(processingJobId);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSubmissionRequest(Guid.Empty, "ABC Manufacturing");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<CreateSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("Submission.InvalidTenant", "Tenant ID is required.")));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Tenant ID");
    }

    [Fact]
    public async Task Assign_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var underwriterId = Guid.NewGuid();
        var request = new AssignSubmissionRequest(underwriterId, "John Underwriter");

        _mediatorMock.Setup(m => m.Send(
                It.Is<AssignSubmissionCommand>(c =>
                    c.SubmissionId == submissionId &&
                    c.UnderwriterId == underwriterId &&
                    c.UnderwriterName == "John Underwriter"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Assign(submissionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Assign_WithNonExistentSubmission_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new AssignSubmissionRequest(Guid.NewGuid(), "John Underwriter");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<AssignSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("Submission.NotFound", "Submission not found")));

        // Act
        var result = await _controller.Assign(submissionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Assign_WithClosedSubmission_ReturnsBadRequest()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new AssignSubmissionRequest(Guid.NewGuid(), "John Underwriter");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<AssignSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error(
                "Submission.CannotAssignClosedSubmission",
                "Cannot assign a closed submission")));

        // Act
        var result = await _controller.Assign(submissionId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    private SubmissionDto CreateSubmissionDto(Guid id) =>
        new(
            Id: id,
            TenantId: _tenantId,
            SubmissionNumber: "SUB-2024-000001",
            Insured: new InsuredPartyDto(
                Id: Guid.NewGuid(),
                Name: "ABC Manufacturing",
                DbaName: null,
                FeinNumber: null,
                MailingAddress: null,
                Industry: null,
                Website: null,
                YearsInBusiness: null,
                EmployeeCount: null,
                AnnualRevenue: null),
            Status: "Received",
            ReceivedAt: DateTime.UtcNow.AddHours(-1),
            EffectiveDate: null,
            ExpirationDate: null,
            ProducerName: null,
            AssignedUnderwriterName: null,
            AssignedAt: null,
            AppetiteScore: null,
            WinnabilityScore: null,
            DataQualityScore: null,
            Coverages: Array.Empty<CoverageDto>(),
            Locations: Array.Empty<ExposureLocationDto>(),
            Losses: Array.Empty<LossHistoryDto>(),
            TotalIncurredLosses: null);
}
