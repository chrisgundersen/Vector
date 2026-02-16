using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vector.Api.Controllers.v1;
using Vector.Application.UnderwritingGuidelines.Commands;
using Vector.Application.UnderwritingGuidelines.DTOs;
using Vector.Application.UnderwritingGuidelines.Queries;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Api.IntegrationTests.Controllers;

public class GuidelinesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GuidelinesController _controller;

    public GuidelinesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new GuidelinesController(_mediatorMock.Object);
    }

    [Fact]
    public async Task List_WithNoFilter_ReturnsOkWithGuidelines()
    {
        var guidelines = new List<GuidelineSummaryDto>
        {
            new(Guid.NewGuid(), "GL Standard", "Description", "Active",
                DateTime.Today, DateTime.Today.AddYears(1), 1, 2, DateTime.UtcNow)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGuidelinesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guidelines);

        var result = await _controller.List(null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(guidelines);
    }

    [Fact]
    public async Task List_WithStatusFilter_PassesFilterToQuery()
    {
        _mediatorMock.Setup(m => m.Send(
                It.Is<GetGuidelinesQuery>(q => q.Status == GuidelineStatus.Active),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GuidelineSummaryDto>());

        await _controller.List(GuidelineStatus.Active);

        _mediatorMock.Verify(m => m.Send(
            It.Is<GetGuidelinesQuery>(q => q.Status == GuidelineStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithExistingGuideline_ReturnsOk()
    {
        var guidelineId = Guid.NewGuid();
        var guideline = new GuidelineDto(guidelineId, Guid.NewGuid(), "GL Standard", "Description",
            "Active", DateTime.Today, DateTime.Today.AddYears(1), 1,
            "GeneralLiability", "CA,TX", "54", [], DateTime.UtcNow, null, null, null);

        _mediatorMock.Setup(m => m.Send(It.Is<GetGuidelineQuery>(q => q.Id == guidelineId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        var result = await _controller.GetById(guidelineId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(guideline);
    }

    [Fact]
    public async Task GetById_WithNonExistentGuideline_ReturnsNotFound()
    {
        var guidelineId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.Is<GetGuidelineQuery>(q => q.Id == guidelineId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuidelineDto?)null);

        var result = await _controller.GetById(guidelineId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateGuidelineRequest("New Guideline", "Description",
            "GeneralLiability", "CA", "54", DateTime.Today, DateTime.Today.AddYears(1));
        var guidelineId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateGuidelineCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(guidelineId));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().Be(guidelineId);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new CreateGuidelineRequest("", null, null, null, null, null, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateGuidelineCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("Guideline.NameRequired", "Name is required.")));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsNoContent()
    {
        var guidelineId = Guid.NewGuid();
        var request = new UpdateGuidelineRequest("Updated Name", "New Desc", "GL", "CA", "54",
            DateTime.Today, DateTime.Today.AddYears(1));

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateGuidelineCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Update(guidelineId, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Activate_WithValidGuideline_ReturnsNoContent()
    {
        var guidelineId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ActivateGuidelineCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Activate(guidelineId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_WithValidGuideline_ReturnsNoContent()
    {
        var guidelineId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeactivateGuidelineCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Deactivate(guidelineId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithValidGuideline_ReturnsNoContent()
    {
        var guidelineId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteGuidelineCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Delete(guidelineId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }
}
