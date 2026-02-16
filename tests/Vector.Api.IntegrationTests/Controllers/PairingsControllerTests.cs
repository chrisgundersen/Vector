using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vector.Api.Controllers.v1;
using Vector.Application.Routing.Commands;
using Vector.Application.Routing.DTOs;
using Vector.Application.Routing.Queries;
using Vector.Domain.Common;

namespace Vector.Api.IntegrationTests.Controllers;

public class PairingsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly PairingsController _controller;

    public PairingsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new PairingsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task List_WithActiveOnlyFilter_ReturnsOk()
    {
        var pairings = new List<PairingSummaryDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Producer A", Guid.NewGuid(), "UW 1", 100, true, DateTime.UtcNow, null, 0)
        };

        _mediatorMock.Setup(m => m.Send(It.Is<GetPairingsQuery>(q => q.ActiveOnly), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pairings);

        var result = await _controller.List(true);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(pairings);
    }

    [Fact]
    public async Task GetById_WithExistingPairing_ReturnsOk()
    {
        var pairingId = Guid.NewGuid();
        var pairing = new PairingDto(pairingId, Guid.NewGuid(), "Producer A", Guid.NewGuid(), "UW 1",
            100, true, DateTime.UtcNow, null, []);

        _mediatorMock.Setup(m => m.Send(It.Is<GetPairingQuery>(q => q.Id == pairingId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pairing);

        var result = await _controller.GetById(pairingId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(pairing);
    }

    [Fact]
    public async Task GetById_WithNonExistentPairing_ReturnsNotFound()
    {
        var pairingId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.Is<GetPairingQuery>(q => q.Id == pairingId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PairingDto?)null);

        var result = await _controller.GetById(pairingId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreatePairingRequest(Guid.NewGuid(), "Producer", Guid.NewGuid(), "UW",
            100, DateTime.UtcNow, null, null);
        var pairingId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreatePairingCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(pairingId));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_ReturnsBadRequest()
    {
        var request = new CreatePairingRequest(Guid.Empty, "", Guid.Empty, "", 100, DateTime.UtcNow, null, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreatePairingCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("Pairing.InvalidProducerId", "Producer ID is required.")));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsNoContent()
    {
        var pairingId = Guid.NewGuid();
        var request = new UpdatePairingRequest(50, DateTime.UtcNow, null, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdatePairingCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Update(pairingId, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_WithNonExistentPairing_ReturnsNotFound()
    {
        var pairingId = Guid.NewGuid();
        var request = new UpdatePairingRequest(50, DateTime.UtcNow, null, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdatePairingCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("Pairing.NotFound", "Not found")));

        var result = await _controller.Update(pairingId, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Activate_WithValidPairing_ReturnsNoContent()
    {
        var pairingId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ActivatePairingCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Activate(pairingId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_WithValidPairing_ReturnsNoContent()
    {
        var pairingId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeactivatePairingCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Deactivate(pairingId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithValidPairing_ReturnsNoContent()
    {
        var pairingId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeletePairingCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Delete(pairingId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }
}
