using FluentAssertions;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;
using Vector.Domain.UnderwritingGuidelines.Events;
using Vector.Domain.UnderwritingGuidelines.ValueObjects;

namespace Vector.Domain.UnitTests.UnderwritingGuidelines;

public class UnderwritingGuidelineTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidName_ReturnsGuideline()
    {
        // Act
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");

        // Assert
        guideline.Should().NotBeNull();
        guideline.Name.Should().Be("Test Guideline");
        guideline.Status.Should().Be(GuidelineStatus.Draft);
        guideline.Version.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsException()
    {
        // Act
        var act = () => UnderwritingGuideline.Create(_tenantId, "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsException()
    {
        // Act
        var act = () => UnderwritingGuideline.Create(Guid.Empty, "Test");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddRule_WithValidRule_AddsRuleToGuideline()
    {
        // Arrange
        var guideline = CreateTestGuideline();

        // Act
        var rule = guideline.AddRule("Test Rule", RuleType.Appetite, RuleAction.Accept);

        // Assert
        guideline.Rules.Should().HaveCount(1);
        guideline.Rules.First().Should().Be(rule);
        rule.Name.Should().Be("Test Rule");
    }

    [Fact]
    public void RemoveRule_ExistingRule_RemovesFromGuideline()
    {
        // Arrange
        var guideline = CreateTestGuideline();
        var rule = guideline.AddRule("Test Rule", RuleType.Appetite, RuleAction.Accept);

        // Act
        guideline.RemoveRule(rule.Id);

        // Assert
        guideline.Rules.Should().BeEmpty();
    }

    [Fact]
    public void Activate_GuidelineWithRules_SetsStatusToActive()
    {
        // Arrange
        var guideline = CreateTestGuideline();
        guideline.AddRule("Test Rule", RuleType.Appetite, RuleAction.Accept);

        // Act
        guideline.Activate();

        // Assert
        guideline.Status.Should().Be(GuidelineStatus.Active);
    }

    [Fact]
    public void Activate_GuidelineWithNoRules_ThrowsException()
    {
        // Arrange
        var guideline = CreateTestGuideline();

        // Act
        var act = () => guideline.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no rules*");
    }

    [Fact]
    public void Deactivate_ActiveGuideline_SetsStatusToInactive()
    {
        // Arrange
        var guideline = CreateTestGuideline();
        guideline.AddRule("Test Rule", RuleType.Appetite, RuleAction.Accept);
        guideline.Activate();

        // Act
        guideline.Deactivate();

        // Assert
        guideline.Status.Should().Be(GuidelineStatus.Inactive);
    }

    [Fact]
    public void SetApplicability_WithValidValues_SetsFilters()
    {
        // Arrange
        var guideline = CreateTestGuideline();

        // Act
        guideline.SetApplicability("GeneralLiability,Property", "CA,TX", "51");

        // Assert
        guideline.ApplicableCoverageTypes.Should().Be("GeneralLiability,Property");
        guideline.ApplicableStates.Should().Contain("CA");
        guideline.ApplicableNAICSCodes.Should().Be("51");
    }

    [Fact]
    public void Create_RaisesGuidelineCreatedEvent()
    {
        // Act
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");

        // Assert
        guideline.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<GuidelineCreatedEvent>();
    }

    [Fact]
    public void Activate_RaisesGuidelineActivatedEvent()
    {
        // Arrange
        var guideline = CreateTestGuideline();
        guideline.AddRule("Test Rule", RuleType.Appetite, RuleAction.Accept);
        guideline.ClearDomainEvents();

        // Act
        guideline.Activate();

        // Assert
        guideline.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<GuidelineActivatedEvent>();
    }

    [Fact]
    public void AddRule_RaisesRuleAddedEvent()
    {
        // Arrange
        var guideline = CreateTestGuideline();
        guideline.ClearDomainEvents();

        // Act
        guideline.AddRule("New Rule", RuleType.Appetite, RuleAction.Accept);

        // Assert
        guideline.DomainEvents.Should().Contain(e => e is RuleAddedEvent);
    }

    [Fact]
    public void Archive_SetsStatusToArchived()
    {
        // Arrange
        var guideline = CreateTestGuideline();

        // Act
        guideline.Archive();

        // Assert
        guideline.Status.Should().Be(GuidelineStatus.Archived);
    }

    [Fact]
    public void AddRule_ArchivedGuideline_ThrowsException()
    {
        // Arrange
        var guideline = CreateTestGuideline();
        guideline.Archive();

        // Act
        var act = () => guideline.AddRule("Test Rule", RuleType.Appetite, RuleAction.Accept);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*archived*");
    }

    private UnderwritingGuideline CreateTestGuideline()
    {
        return UnderwritingGuideline.Create(_tenantId, "Test Guideline");
    }
}
