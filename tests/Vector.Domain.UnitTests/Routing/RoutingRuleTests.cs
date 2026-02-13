using FluentAssertions;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Enums;
using Vector.Domain.Routing.ValueObjects;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Domain.UnitTests.Routing;

public class RoutingRuleTests
{
    [Fact]
    public void Create_WithValidName_ReturnsSuccessResult()
    {
        // Act
        var result = RoutingRule.Create("Test Rule", "Description", RoutingStrategy.Direct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Rule");
        result.Value.Description.Should().Be("Description");
        result.Value.Strategy.Should().Be(RoutingStrategy.Direct);
        result.Value.Status.Should().Be(RoutingRuleStatus.Draft);
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailure()
    {
        // Act
        var result = RoutingRule.Create("", "Description", RoutingStrategy.Direct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingRule.NameRequired");
    }

    [Fact]
    public void Create_WithNameTooLong_ReturnsFailure()
    {
        // Act
        var result = RoutingRule.Create(new string('a', 201), "Description", RoutingStrategy.Direct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingRule.NameTooLong");
    }

    [Fact]
    public void AddCondition_WithValidCondition_AddsToRule()
    {
        // Arrange
        var rule = CreateTestRule();
        var condition = RoutingCondition.Equals(RuleField.InsuredState, "CA");

        // Act
        var result = rule.AddCondition(condition);

        // Assert
        result.IsSuccess.Should().BeTrue();
        rule.Conditions.Should().HaveCount(1);
    }

    [Fact]
    public void AddCondition_WithDuplicateCondition_ReturnsFailure()
    {
        // Arrange
        var rule = CreateTestRule();
        var condition = RoutingCondition.Equals(RuleField.InsuredState, "CA");
        rule.AddCondition(condition);

        // Act
        var result = rule.AddCondition(condition);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingRule.DuplicateCondition");
    }

    [Fact]
    public void SetTargetUnderwriter_SetsUnderwriterAndClearsTeam()
    {
        // Arrange
        var rule = CreateTestRule();
        var underwriterId = Guid.NewGuid();

        // Act
        rule.SetTargetUnderwriter(underwriterId, "John Smith");

        // Assert
        rule.TargetUnderwriterId.Should().Be(underwriterId);
        rule.TargetUnderwriterName.Should().Be("John Smith");
        rule.TargetTeamId.Should().BeNull();
        rule.TargetTeamName.Should().BeNull();
    }

    [Fact]
    public void SetTargetTeam_SetsTeamAndClearsUnderwriter()
    {
        // Arrange
        var rule = CreateTestRule();
        rule.SetTargetUnderwriter(Guid.NewGuid(), "John Smith");
        var teamId = Guid.NewGuid();

        // Act
        rule.SetTargetTeam(teamId, "Property Team");

        // Assert
        rule.TargetTeamId.Should().Be(teamId);
        rule.TargetTeamName.Should().Be("Property Team");
        rule.TargetUnderwriterId.Should().BeNull();
        rule.TargetUnderwriterName.Should().BeNull();
    }

    [Fact]
    public void Activate_WithoutTargetForDirectStrategy_ReturnsFailure()
    {
        // Arrange
        var result = RoutingRule.Create("Direct Rule", "Description", RoutingStrategy.Direct);
        var rule = result.Value;

        // Act
        var activateResult = rule.Activate();

        // Assert
        activateResult.IsFailure.Should().BeTrue();
        activateResult.Error.Code.Should().Be("RoutingRule.TargetRequired");
    }

    [Fact]
    public void Activate_WithTargetForDirectStrategy_Succeeds()
    {
        // Arrange
        var result = RoutingRule.Create("Direct Rule", "Description", RoutingStrategy.Direct);
        var rule = result.Value;
        rule.SetTargetUnderwriter(Guid.NewGuid(), "John Smith");

        // Act
        var activateResult = rule.Activate();

        // Assert
        activateResult.IsSuccess.Should().BeTrue();
        rule.Status.Should().Be(RoutingRuleStatus.Active);
    }

    [Fact]
    public void Activate_AlreadyActiveRule_ReturnsFailure()
    {
        // Arrange
        var rule = CreateTestRule(RoutingStrategy.ManualQueue);
        rule.Activate();

        // Act
        var result = rule.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingRule.AlreadyActive");
    }

    [Fact]
    public void Matches_WithNoConditions_ReturnsTrue()
    {
        // Arrange
        var rule = CreateTestRule(RoutingStrategy.ManualQueue);
        rule.Activate();
        var fieldValues = new Dictionary<RuleField, string?>
        {
            [RuleField.InsuredState] = "CA"
        };

        // Act
        var matches = rule.Matches(fieldValues);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Matches_WithMatchingConditions_ReturnsTrue()
    {
        // Arrange
        var rule = CreateTestRule(RoutingStrategy.ManualQueue);
        rule.AddCondition(RoutingCondition.Equals(RuleField.InsuredState, "CA"));
        rule.Activate();
        var fieldValues = new Dictionary<RuleField, string?>
        {
            [RuleField.InsuredState] = "CA"
        };

        // Act
        var matches = rule.Matches(fieldValues);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Matches_WithNonMatchingConditions_ReturnsFalse()
    {
        // Arrange
        var rule = CreateTestRule(RoutingStrategy.ManualQueue);
        rule.AddCondition(RoutingCondition.Equals(RuleField.InsuredState, "CA"));
        rule.Activate();
        var fieldValues = new Dictionary<RuleField, string?>
        {
            [RuleField.InsuredState] = "TX"
        };

        // Act
        var matches = rule.Matches(fieldValues);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Matches_InactiveRule_ReturnsFalse()
    {
        // Arrange
        var rule = CreateTestRule(RoutingStrategy.ManualQueue);
        // Not activated
        var fieldValues = new Dictionary<RuleField, string?>
        {
            [RuleField.InsuredState] = "CA"
        };

        // Act
        var matches = rule.Matches(fieldValues);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Create_RaisesRoutingRuleCreatedEvent()
    {
        // Act
        var result = RoutingRule.Create("Test Rule", "Description", RoutingStrategy.Direct);

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RoutingRuleCreatedEvent>();
    }

    [Fact]
    public void SetPriority_WithValidValue_UpdatesPriority()
    {
        // Arrange
        var rule = CreateTestRule();

        // Act
        rule.SetPriority(50);

        // Assert
        rule.Priority.Should().Be(50);
    }

    [Fact]
    public void SetPriority_WithInvalidValue_ThrowsException()
    {
        // Arrange
        var rule = CreateTestRule();

        // Act & Assert
        var act = () => rule.SetPriority(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static RoutingRule CreateTestRule(RoutingStrategy strategy = RoutingStrategy.Direct)
    {
        var result = RoutingRule.Create("Test Rule", "Description", strategy);
        return result.Value;
    }
}
