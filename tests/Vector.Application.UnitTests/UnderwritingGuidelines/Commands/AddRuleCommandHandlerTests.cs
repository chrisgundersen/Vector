using Vector.Application.Common.Interfaces;
using Vector.Application.UnderwritingGuidelines.Commands;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Application.UnitTests.UnderwritingGuidelines.Commands;

public class AddRuleCommandHandlerTests
{
    private readonly Mock<IUnderwritingGuidelineRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly AddRuleCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public AddRuleCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUnderwritingGuidelineRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _currentUserServiceMock.Setup(x => x.TenantId).Returns(_tenantId);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(x => x.UnitOfWork).Returns(unitOfWorkMock.Object);

        _handler = new AddRuleCommandHandler(
            _repositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccessWithRuleId()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");

        var command = new AddRuleCommand(
            guidelineId,
            "Test Rule",
            "Rule description",
            RuleType.Appetite,
            RuleAction.Accept,
            0, null, null, "Accepted");

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _repositoryMock.Verify(x => x.Update(It.IsAny<UnderwritingGuideline>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithScoreAdjustment_SetsCorrectValue()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");

        var command = new AddRuleCommand(
            guidelineId,
            "Score Rule",
            null,
            RuleType.Appetite,
            RuleAction.AdjustScore,
            0, 25, null, null);

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        guideline.Rules.Should().HaveCount(1);
        guideline.Rules.First().ScoreAdjustment.Should().Be(25);
    }

    [Fact]
    public async Task Handle_WithPricingModifier_SetsCorrectValue()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");

        var command = new AddRuleCommand(
            guidelineId,
            "Pricing Rule",
            null,
            RuleType.Pricing,
            RuleAction.ApplyModifier,
            0, null, 1.25m, null);

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        guideline.Rules.Should().HaveCount(1);
        guideline.Rules.First().PricingModifier.Should().Be(1.25m);
    }

    [Fact]
    public async Task Handle_WithNonExistentGuideline_ReturnsFailure()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var command = new AddRuleCommand(
            guidelineId,
            "Test Rule",
            null,
            RuleType.Appetite,
            RuleAction.Accept,
            0, null, null, null);

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UnderwritingGuideline?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.NotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidScoreAdjustment_ReturnsFailure()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");

        var command = new AddRuleCommand(
            guidelineId,
            "Invalid Score Rule",
            null,
            RuleType.Appetite,
            RuleAction.AdjustScore,
            0, 150, null, null); // Score adjustment out of range

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.InvalidRuleValue");
    }

    [Fact]
    public async Task Handle_WithArchivedGuideline_ReturnsFailure()
    {
        // Arrange
        var guidelineId = Guid.NewGuid();
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");
        guideline.Archive();

        var command = new AddRuleCommand(
            guidelineId,
            "Test Rule",
            null,
            RuleType.Appetite,
            RuleAction.Accept,
            0, null, null, null);

        _repositoryMock.Setup(x => x.GetByIdWithRulesAsync(guidelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Guideline.RuleAddFailed");
    }
}
