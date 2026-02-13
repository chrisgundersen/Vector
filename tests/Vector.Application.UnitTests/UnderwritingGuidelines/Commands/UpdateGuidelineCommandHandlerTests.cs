using Vector.Application.Common.Interfaces;
using Vector.Application.UnderwritingGuidelines.Commands;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;
using Vector.Domain.UnderwritingGuidelines.Aggregates;

namespace Vector.Application.UnitTests.UnderwritingGuidelines.Commands;

public class UpdateGuidelineCommandHandlerTests
{
    private readonly Mock<IUnderwritingGuidelineRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly UpdateGuidelineCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateGuidelineCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUnderwritingGuidelineRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _currentUserServiceMock.Setup(x => x.TenantId).Returns(_tenantId);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(x => x.UnitOfWork).Returns(unitOfWorkMock.Object);

        _handler = new UpdateGuidelineCommandHandler(
            _repositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Original Name");

        var command = new UpdateGuidelineCommand(
            guidelineId,
            "Updated Name",
            "Updated description",
            "GL", "CA", "44",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(365));

        _repositoryMock.Setup(x => x.GetByIdAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        _repositoryMock.Setup(x => x.ExistsByNameAsync(
                _tenantId, "Updated Name", guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.Update(It.IsAny<UnderwritingGuideline>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentGuideline_ReturnsFailure()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var command = new UpdateGuidelineCommand(
            guidelineId,
            "Updated Name",
            null, null, null, null, null, null);

        _repositoryMock.Setup(x => x.GetByIdAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UnderwritingGuideline?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.NotFound");
    }

    [Fact]
    public async Task Handle_WithDifferentTenant_ReturnsNotFound()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(differentTenantId, "Test Guideline");

        var command = new UpdateGuidelineCommand(
            guidelineId,
            "Updated Name",
            null, null, null, null, null, null);

        _repositoryMock.Setup(x => x.GetByIdAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.NotFound");
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ReturnsFailure()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Original Name");

        var command = new UpdateGuidelineCommand(
            guidelineId,
            "Existing Name",
            null, null, null, null, null, null);

        _repositoryMock.Setup(x => x.GetByIdAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        _repositoryMock.Setup(x => x.ExistsByNameAsync(
                _tenantId, "Existing Name", guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.DuplicateName");
    }
}
