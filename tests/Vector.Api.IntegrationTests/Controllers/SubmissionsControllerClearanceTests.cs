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

public class SubmissionsControllerClearanceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<SubmissionsController>> _loggerMock;
    private readonly SubmissionsController _controller;

    private readonly Guid _tenantId = Guid.NewGuid();

    public SubmissionsControllerClearanceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SubmissionsController>>();
        _controller = new SubmissionsController(_mediatorMock.Object, _loggerMock.Object);
    }

    #region Clearance Queue Tests

    [Fact]
    public async Task GetClearanceQueue_ReturnsOkWithSubmissions()
    {
        var submissions = new List<SubmissionSummaryDto>
        {
            new(Guid.NewGuid(), "SUB-2024-000001", "Test Insured", "PendingClearance",
                DateTime.UtcNow, null, null, null, null, null, 0, 0, null, "Failed")
        };

        _mediatorMock.Setup(m => m.Send(
                It.Is<GetClearanceQueueQuery>(q => q.Limit == 50),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissions.AsReadOnly());

        var result = await _controller.GetClearanceQueue(cancellationToken: CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetClearanceQueue_WithCustomLimit_PassesLimitToQuery()
    {
        _mediatorMock.Setup(m => m.Send(
                It.Is<GetClearanceQueueQuery>(q => q.Limit == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubmissionSummaryDto>().AsReadOnly());

        var result = await _controller.GetClearanceQueue(limit: 10, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetClearanceQueueQuery>(q => q.Limit == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Override Clearance Tests

    [Fact]
    public async Task OverrideClearance_WithValidRequest_ReturnsNoContent()
    {
        var submissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new OverrideClearanceRequest("Confirmed not a duplicate", userId);

        _mediatorMock.Setup(m => m.Send(
                It.Is<OverrideClearanceCommand>(c =>
                    c.SubmissionId == submissionId &&
                    c.Reason == "Confirmed not a duplicate" &&
                    c.OverriddenByUserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.OverrideClearance(submissionId, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task OverrideClearance_WhenNotFound_ReturnsNotFound()
    {
        var submissionId = Guid.NewGuid();
        var request = new OverrideClearanceRequest("Reason", Guid.NewGuid());

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<OverrideClearanceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("Submission.NotFound", "Submission not found")));

        var result = await _controller.OverrideClearance(submissionId, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OverrideClearance_WhenNotPendingClearance_ReturnsBadRequest()
    {
        var submissionId = Guid.NewGuid();
        var request = new OverrideClearanceRequest("Reason", Guid.NewGuid());

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<OverrideClearanceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error(
                "Submission.NotPendingClearance",
                "Submission is not pending clearance.")));

        var result = await _controller.OverrideClearance(submissionId, request, CancellationToken.None);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Withdraw Tests

    [Fact]
    public async Task Withdraw_WithValidRequest_ReturnsNoContent()
    {
        var submissionId = Guid.NewGuid();
        var request = new WithdrawSubmissionRequest("Customer changed mind");

        _mediatorMock.Setup(m => m.Send(
                It.Is<WithdrawSubmissionCommand>(c =>
                    c.SubmissionId == submissionId &&
                    c.Reason == "Customer changed mind"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Withdraw(submissionId, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Withdraw_WhenNotFound_ReturnsNotFound()
    {
        var submissionId = Guid.NewGuid();
        var request = new WithdrawSubmissionRequest("Reason");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<WithdrawSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("Submission.NotFound", "Submission not found")));

        var result = await _controller.Withdraw(submissionId, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Withdraw_WhenCannotWithdraw_ReturnsBadRequest()
    {
        var submissionId = Guid.NewGuid();
        var request = new WithdrawSubmissionRequest("Too late");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<WithdrawSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error(
                "Submission.CannotWithdrawClosedSubmission",
                "Cannot withdraw a bound submission")));

        var result = await _controller.Withdraw(submissionId, request, CancellationToken.None);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Expire Tests

    [Fact]
    public async Task Expire_WithValidRequest_ReturnsNoContent()
    {
        var submissionId = Guid.NewGuid();
        var request = new ExpireSubmissionRequest("Policy period passed");

        _mediatorMock.Setup(m => m.Send(
                It.Is<ExpireSubmissionCommand>(c =>
                    c.SubmissionId == submissionId &&
                    c.Reason == "Policy period passed"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Expire(submissionId, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Expire_WhenNotFound_ReturnsNotFound()
    {
        var submissionId = Guid.NewGuid();
        var request = new ExpireSubmissionRequest("Reason");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<ExpireSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("Submission.NotFound", "Submission not found")));

        var result = await _controller.Expire(submissionId, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Expire_WhenCannotExpire_ReturnsBadRequest()
    {
        var submissionId = Guid.NewGuid();
        var request = new ExpireSubmissionRequest("Too late");

        _mediatorMock.Setup(m => m.Send(
                It.IsAny<ExpireSubmissionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error(
                "Submission.CannotExpire",
                "Cannot expire a bound submission")));

        var result = await _controller.Expire(submissionId, request, CancellationToken.None);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion
}
