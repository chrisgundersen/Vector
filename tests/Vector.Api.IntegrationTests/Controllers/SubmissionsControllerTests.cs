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

    #region Quote Endpoint Tests

    [Fact]
    public async Task Quote_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new QuoteSubmissionRequest(25000m, "USD");

        _mediatorMock.Setup(m => m.Send(
                It.Is<QuoteSubmissionCommand>(c =>
                    c.SubmissionId == submissionId &&
                    c.PremiumAmount == 25000m &&
                    c.Currency == "USD"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Quote(submissionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Quote_WithNonExistentSubmission_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new QuoteSubmissionRequest(25000m, "USD");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<QuoteSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("Submission.NotFound", "Submission not found")));

        // Act
        var result = await _controller.Quote(submissionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Quote_WithInvalidSubmissionState_ReturnsBadRequest()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new QuoteSubmissionRequest(25000m, "USD");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<QuoteSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error(
                "Submission.InvalidState",
                "Cannot quote a submission that is not in review")));

        // Act
        var result = await _controller.Quote(submissionId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Decline Endpoint Tests

    [Fact]
    public async Task Decline_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new DeclineSubmissionRequest("Outside appetite - geographic restrictions");

        _mediatorMock.Setup(m => m.Send(
                It.Is<DeclineSubmissionCommand>(c =>
                    c.SubmissionId == submissionId &&
                    c.Reason == "Outside appetite - geographic restrictions"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Decline(submissionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Decline_WithNonExistentSubmission_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new DeclineSubmissionRequest("Not in appetite");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<DeclineSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("Submission.NotFound", "Submission not found")));

        // Act
        var result = await _controller.Decline(submissionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Decline_WithAlreadyBoundSubmission_ReturnsBadRequest()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new DeclineSubmissionRequest("Changed our mind");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<DeclineSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error(
                "Submission.AlreadyBound",
                "Cannot decline a bound submission")));

        // Act
        var result = await _controller.Decline(submissionId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region RequestInfo Endpoint Tests

    [Fact]
    public async Task RequestInfo_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new RequestInfoRequest("Please provide 5-year loss history");

        _mediatorMock.Setup(m => m.Send(
                It.Is<RequestInformationCommand>(c =>
                    c.SubmissionId == submissionId &&
                    c.Reason == "Please provide 5-year loss history"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.RequestInfo(submissionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RequestInfo_WithNonExistentSubmission_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new RequestInfoRequest("Need more details");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<RequestInformationCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("Submission.NotFound", "Submission not found")));

        // Act
        var result = await _controller.RequestInfo(submissionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RequestInfo_WithClosedSubmission_ReturnsBadRequest()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new RequestInfoRequest("Additional documents needed");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<RequestInformationCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error(
                "Submission.Closed",
                "Cannot request information for a closed submission")));

        // Act
        var result = await _controller.RequestInfo(submissionId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Bind Endpoint Tests

    [Fact]
    public async Task Bind_WithValidRequest_ReturnsOkWithResult()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var bindResult = new BindSubmissionResult(
            submissionId,
            "SUB-2024-000001",
            "EXT-POL-001",
            "POL-2024-000001",
            DateTime.UtcNow);

        _mediatorMock.Setup(m => m.Send(
                It.Is<BindSubmissionCommand>(c => c.SubmissionId == submissionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(bindResult));

        // Act
        var result = await _controller.Bind(submissionId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var returnedResult = okResult.Value.Should().BeOfType<BindSubmissionResult>().Subject;
        returnedResult.SubmissionNumber.Should().Be("SUB-2024-000001");
        returnedResult.ExternalPolicyId.Should().Be("EXT-POL-001");
        returnedResult.PolicyNumber.Should().Be("POL-2024-000001");
    }

    [Fact]
    public async Task Bind_WithNonExistentSubmission_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<BindSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<BindSubmissionResult>(
                new Error("Submission.NotFound", "Submission not found")));

        // Act
        var result = await _controller.Bind(submissionId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Bind_WithNonQuotedSubmission_ReturnsBadRequest()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<BindSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<BindSubmissionResult>(new Error(
                "Submission.NotQuoted",
                "Only quoted submissions can be bound")));

        // Act
        var result = await _controller.Bind(submissionId, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Bind_WhenPasFails_StillReturnsSuccessWithPartialResult()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var bindResult = new BindSubmissionResult(
            submissionId,
            "SUB-2024-000001",
            null, // PAS failed, no external policy ID
            null, // No policy number
            DateTime.UtcNow);

        _mediatorMock.Setup(m => m.Send(
                It.Is<BindSubmissionCommand>(c => c.SubmissionId == submissionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(bindResult));

        // Act
        var result = await _controller.Bind(submissionId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<BindSubmissionResult>().Subject;
        returnedResult.ExternalPolicyId.Should().BeNull();
        returnedResult.PolicyNumber.Should().BeNull();
    }

    #endregion

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
            TotalIncurredLosses: null,
            ClearanceStatus: "Passed",
            ClearanceCheckedAt: DateTime.UtcNow.AddHours(-1),
            ClearanceMatches: Array.Empty<ClearanceMatchDto>(),
            ClearanceOverrideReason: null);
}
