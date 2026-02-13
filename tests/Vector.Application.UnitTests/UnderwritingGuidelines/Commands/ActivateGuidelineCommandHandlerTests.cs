using Vector.Application.Common.Interfaces;
using Vector.Application.UnderwritingGuidelines.Commands;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Application.UnitTests.UnderwritingGuidelines.Commands;

public class ActivateGuidelineCommandHandlerTests
{
    private readonly Mock<IUnderwritingGuidelineRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly ActivateGuidelineCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public ActivateGuidelineCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUnderwritingGuidelineRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _currentUserServiceMock.Setup(x => x.TenantId).Returns(_tenantId);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(x => x.UnitOfWork).Returns(unitOfWorkMock.Object);

        _handler = new ActivateGuidelineCommandHandler(
            _repositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidGuidelineWithRules_ReturnsSuccess()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");
        guideline.AddRule("Test Rule", RuleType.Appetite, RuleAction.Accept, 0);

        var command = new ActivateGuidelineCommand(guidelineId);

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.Update(It.IsAny<UnderwritingGuideline>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithGuidelineWithNoRules_ReturnsFailure()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");

        var command = new ActivateGuidelineCommand(guidelineId);

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.ActivationFailed");
    }

    [Fact]
    public async Task Handle_WithNonExistentGuideline_ReturnsFailure()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var command = new ActivateGuidelineCommand(guidelineId);

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
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
        guideline.AddRule("Test Rule", RuleType.Appetite, RuleAction.Accept, 0);

        var command = new ActivateGuidelineCommand(guidelineId);

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.NotFound");
    }
}
