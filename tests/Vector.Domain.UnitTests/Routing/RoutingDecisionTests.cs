using FluentAssertions;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Enums;

namespace Vector.Domain.UnitTests.Routing;

public class RoutingDecisionTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        // Act
        var result = RoutingDecision.Create(submissionId, "SUB-001", RoutingStrategy.Direct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SubmissionId.Should().Be(submissionId);
        result.Value.SubmissionNumber.Should().Be("SUB-001");
        result.Value.Strategy.Should().Be(RoutingStrategy.Direct);
        result.Value.Status.Should().Be(RoutingDecisionStatus.Pending);
    }

    [Fact]
    public void Create_WithEmptySubmissionId_ReturnsFailure()
    {
        // Act
        var result = RoutingDecision.Create(Guid.Empty, "SUB-001", RoutingStrategy.Direct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingDecision.SubmissionIdRequired");
    }

    [Fact]
    public void Create_WithEmptySubmissionNumber_ReturnsFailure()
    {
        // Act
        var result = RoutingDecision.Create(Guid.NewGuid(), "", RoutingStrategy.Direct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingDecision.SubmissionNumberRequired");
    }

    [Fact]
    public void AssignToUnderwriter_PendingDecision_AssignsSuccessfully()
    {
        // Arrange
        var decision = CreateTestDecision();
        var underwriterId = Guid.NewGuid();

        // Act
        var result = decision.AssignToUnderwriter(underwriterId, "John Smith");

        // Assert
        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(RoutingDecisionStatus.Assigned);
        decision.AssignedUnderwriterId.Should().Be(underwriterId);
        decision.AssignedUnderwriterName.Should().Be("John Smith");
        decision.AssignedAt.Should().NotBeNull();
    }

    [Fact]
    public void AssignToUnderwriter_AlreadyAssigned_ReassignsWithHistory()
    {
        // Arrange
        var decision = CreateTestDecision();
        var firstUnderwriterId = Guid.NewGuid();
        var secondUnderwriterId = Guid.NewGuid();
        decision.AssignToUnderwriter(firstUnderwriterId, "First UW");

        // Act
        var result = decision.AssignToUnderwriter(secondUnderwriterId, "Second UW");

        // Assert
        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(RoutingDecisionStatus.Reassigned);
        decision.AssignedUnderwriterId.Should().Be(secondUnderwriterId);
        decision.AssignedUnderwriterName.Should().Be("Second UW");
        decision.History.Should().HaveCount(2);
    }

    [Fact]
    public void AssignToUnderwriter_AcceptedDecision_ReturnsFailure()
    {
        // Arrange
        var decision = CreateTestDecision();
        decision.AssignToUnderwriter(Guid.NewGuid(), "First UW");
        decision.Accept();

        // Act
        var result = decision.AssignToUnderwriter(Guid.NewGuid(), "Second UW");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingDecision.CannotReassign");
    }

    [Fact]
    public void Accept_AssignedDecision_SetsStatusToAccepted()
    {
        // Arrange
        var decision = CreateTestDecision();
        decision.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Act
        var result = decision.Accept("Looks good");

        // Assert
        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(RoutingDecisionStatus.Accepted);
        decision.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public void Accept_PendingDecision_ReturnsFailure()
    {
        // Arrange
        var decision = CreateTestDecision();

        // Act
        var result = decision.Accept();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingDecision.NotAssigned");
    }

    [Fact]
    public void Decline_AssignedDecision_SetsStatusToDeclined()
    {
        // Arrange
        var decision = CreateTestDecision();
        decision.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Act
        var result = decision.Decline("Outside appetite");

        // Assert
        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(RoutingDecisionStatus.Declined);
        decision.DeclinedAt.Should().NotBeNull();
        decision.DeclineReason.Should().Be("Outside appetite");
    }

    [Fact]
    public void Decline_WithoutReason_ReturnsFailure()
    {
        // Arrange
        var decision = CreateTestDecision();
        decision.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Act
        var result = decision.Decline("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingDecision.DeclineReasonRequired");
    }

    [Fact]
    public void Decline_PendingDecision_ReturnsFailure()
    {
        // Arrange
        var decision = CreateTestDecision();

        // Act
        var result = decision.Decline("Not assigned yet");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoutingDecision.NotAssigned");
    }

    [Fact]
    public void Escalate_SetsStatusToEscalated()
    {
        // Arrange
        var decision = CreateTestDecision();

        // Act
        decision.Escalate("Needs manager approval");

        // Assert
        decision.Status.Should().Be(RoutingDecisionStatus.Escalated);
        decision.History.Should().ContainSingle()
            .Which.Action.Should().Be("Escalated");
    }

    [Fact]
    public void AssignToTeam_SetsTeamAndClearsUnderwriter()
    {
        // Arrange
        var decision = CreateTestDecision();
        decision.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        var teamId = Guid.NewGuid();

        // Act
        var result = decision.AssignToTeam(teamId, "Property Team");

        // Assert
        result.IsSuccess.Should().BeTrue();
        decision.AssignedTeamId.Should().Be(teamId);
        decision.AssignedTeamName.Should().Be("Property Team");
        decision.AssignedUnderwriterId.Should().BeNull();
    }

    [Fact]
    public void SetScores_SetsAppetiteAndWinnabilityScores()
    {
        // Arrange
        var decision = CreateTestDecision();

        // Act
        decision.SetScores(85, 72);

        // Assert
        decision.AppetiteScore.Should().Be(85);
        decision.WinnabilityScore.Should().Be(72);
    }

    [Fact]
    public void SetMatchedRule_SetsRuleInfo()
    {
        // Arrange
        var decision = CreateTestDecision();
        var ruleId = Guid.NewGuid();

        // Act
        decision.SetMatchedRule(ruleId, "High Value Property Rule");

        // Assert
        decision.MatchedRuleId.Should().Be(ruleId);
        decision.MatchedRuleName.Should().Be("High Value Property Rule");
    }

    [Fact]
    public void Create_RaisesRoutingDecisionCreatedEvent()
    {
        // Act
        var result = RoutingDecision.Create(Guid.NewGuid(), "SUB-001", RoutingStrategy.Direct);

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RoutingDecisionCreatedEvent>();
    }

    [Fact]
    public void AssignToUnderwriter_RaisesSubmissionAssignedEvent()
    {
        // Arrange
        var decision = CreateTestDecision();
        decision.ClearDomainEvents();
        var underwriterId = Guid.NewGuid();

        // Act
        decision.AssignToUnderwriter(underwriterId, "John Smith");

        // Assert
        decision.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SubmissionAssignedEvent>();
    }

    [Fact]
    public void Accept_RaisesRoutingAcceptedEvent()
    {
        // Arrange
        var decision = CreateTestDecision();
        decision.AssignToUnderwriter(Guid.NewGuid(), "John Smith");
        decision.ClearDomainEvents();

        // Act
        decision.Accept();

        // Assert
        decision.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RoutingAcceptedEvent>();
    }

    private static RoutingDecision CreateTestDecision()
    {
        var result = RoutingDecision.Create(Guid.NewGuid(), "SUB-001", RoutingStrategy.Direct);
        return result.Value;
    }
}
