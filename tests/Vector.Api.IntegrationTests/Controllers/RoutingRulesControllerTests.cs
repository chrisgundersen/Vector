using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vector.Api.Controllers.v1;
using Vector.Application.Routing.Commands;
using Vector.Application.Routing.DTOs;
using Vector.Application.Routing.Queries;
using Vector.Domain.Common;
using Vector.Domain.Routing.Enums;

namespace Vector.Api.IntegrationTests.Controllers;

public class RoutingRulesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly RoutingRulesController _controller;

    public RoutingRulesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new RoutingRulesController(_mediatorMock.Object);
    }

    [Fact]
    public async Task List_WithNoFilter_ReturnsOkWithRules()
    {
        var rules = new List<RoutingRuleSummaryDto>
        {
            new(Guid.NewGuid(), "Rule 1", "Description", 100, "Active", "Direct", "John", null, 2, DateTime.UtcNow)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetRoutingRulesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var result = await _controller.List(null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(rules);
    }

    [Fact]
    public async Task GetById_WithExistingRule_ReturnsOk()
    {
        var ruleId = Guid.NewGuid();
        var rule = new RoutingRuleDto(ruleId, "Rule 1", "Desc", 100, "Active", "Direct",
            Guid.NewGuid(), "John", null, null, [], DateTime.UtcNow, null, null);

        _mediatorMock.Setup(m => m.Send(It.Is<GetRoutingRuleQuery>(q => q.Id == ruleId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var result = await _controller.GetById(ruleId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(rule);
    }

    [Fact]
    public async Task GetById_WithNonExistentRule_ReturnsNotFound()
    {
        var ruleId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.Is<GetRoutingRuleQuery>(q => q.Id == ruleId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoutingRuleDto?)null);

        var result = await _controller.GetById(ruleId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateRoutingRuleRequest("New Rule", "Desc", RoutingStrategy.Direct, 100,
            Guid.NewGuid(), "John", null, null);
        var ruleId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateRoutingRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ruleId));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().Be(ruleId);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new CreateRoutingRuleRequest("", "Desc", RoutingStrategy.Direct, 100, null, null, null, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateRoutingRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("RoutingRule.NameRequired", "Name is required.")));

        var result = await _controller.Create(request, CancellationToken.None);

        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsNoContent()
    {
        var ruleId = Guid.NewGuid();
        var request = new UpdateRoutingRuleRequest("Updated", "Desc", RoutingStrategy.Direct, 50,
            Guid.NewGuid(), "John", null, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateRoutingRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Update(ruleId, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_WithNonExistentRule_ReturnsNotFound()
    {
        var ruleId = Guid.NewGuid();
        var request = new UpdateRoutingRuleRequest("Updated", "Desc", RoutingStrategy.Direct, 50, null, null, null, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateRoutingRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("RoutingRule.NotFound", "Not found")));

        var result = await _controller.Update(ruleId, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Activate_WithValidRule_ReturnsNoContent()
    {
        var ruleId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ActivateRoutingRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Activate(ruleId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_WithValidRule_ReturnsNoContent()
    {
        var ruleId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeactivateRoutingRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Deactivate(ruleId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithValidRule_ReturnsNoContent()
    {
        var ruleId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteRoutingRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Delete(ruleId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }
}
