using Vector.Application.Common.Interfaces;
using Vector.Application.UnderwritingGuidelines.Commands;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;
using Vector.Domain.UnderwritingGuidelines.Aggregates;

namespace Vector.Application.UnitTests.UnderwritingGuidelines.Commands;

public class CreateGuidelineCommandHandlerTests
{
    private readonly Mock<IUnderwritingGuidelineRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly CreateGuidelineCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateGuidelineCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUnderwritingGuidelineRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _currentUserServiceMock.Setup(x => x.TenantId).Returns(_tenantId);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(x => x.UnitOfWork).Returns(unitOfWorkMock.Object);

        _handler = new CreateGuidelineCommandHandler(
            _repositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccessWithGuidelineId()
    {
        // Arrange
        var command = new CreateGuidelineCommand(
            "Test Guideline",
            "Test description",
            null, null, null, null, null);

        _repositoryMock.Setup(x => x.ExistsByNameAsync(
                _tenantId, "Test Guideline", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(x => x.AddAsync(
                It.IsAny<UnderwritingGuideline>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _repositoryMock.Verify(x => x.AddAsync(
            It.Is<UnderwritingGuideline>(g =>
                g.TenantId == _tenantId &&
                g.Name == "Test Guideline" &&
                g.Description == "Test description"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateGuidelineCommand(
            "Existing Guideline",
            null, null, null, null, null, null);

        _repositoryMock.Setup(x => x.ExistsByNameAsync(
                _tenantId, "Existing Guideline", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.DuplicateName");

        _repositoryMock.Verify(x => x.AddAsync(
            It.IsAny<UnderwritingGuideline>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithApplicability_SetsCorrectValues()
    {
        // Arrange
        var command = new CreateGuidelineCommand(
            "Test Guideline",
            null,
            "GL, Property",
            "CA, TX",
            "44, 45",
            null, null);

        _repositoryMock.Setup(x => x.ExistsByNameAsync(
                _tenantId, "Test Guideline", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UnderwritingGuideline? capturedGuideline = null;
        _repositoryMock.Setup(x => x.AddAsync(
                It.IsAny<UnderwritingGuideline>(), It.IsAny<CancellationToken>()))
            .Callback<UnderwritingGuideline, CancellationToken>((g, _) => capturedGuideline = g)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedGuideline.Should().NotBeNull();
        capturedGuideline!.ApplicableCoverageTypes.Should().Be("GL, Property");
        capturedGuideline!.ApplicableStates.Should().Be("CA, TX");
        capturedGuideline!.ApplicableNAICSCodes.Should().Be("44, 45");
    }

    [Fact]
    public async Task Handle_WithEffectiveDates_SetsCorrectValues()
    {
        // Arrange
        var effectiveDate = DateTime.UtcNow.AddDays(1);
        var expirationDate = DateTime.UtcNow.AddDays(365);

        var command = new CreateGuidelineCommand(
            "Test Guideline",
            null, null, null, null,
            effectiveDate,
            expirationDate);

        _repositoryMock.Setup(x => x.ExistsByNameAsync(
                _tenantId, "Test Guideline", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UnderwritingGuideline? capturedGuideline = null;
        _repositoryMock.Setup(x => x.AddAsync(
                It.IsAny<UnderwritingGuideline>(), It.IsAny<CancellationToken>()))
            .Callback<UnderwritingGuideline, CancellationToken>((g, _) => capturedGuideline = g)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedGuideline.Should().NotBeNull();
        capturedGuideline!.EffectiveDate.Should().Be(effectiveDate);
        capturedGuideline!.ExpirationDate.Should().Be(expirationDate);
    }

    [Fact]
    public async Task Handle_WithNoTenantId_ThrowsException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.TenantId).Returns((Guid?)null);

        var command = new CreateGuidelineCommand(
            "Test Guideline",
            null, null, null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}
