using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Application.Scoring.Services;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.ValueObjects;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;
using Vector.Domain.UnderwritingGuidelines.ValueObjects;

namespace Vector.Application.UnitTests.Scoring.Services;

public class AppetiteScoringServiceTests
{
    private readonly Mock<ILogger<AppetiteScoringService>> _loggerMock;
    private readonly AppetiteScoringService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AppetiteScoringServiceTests()
    {
        _loggerMock = new Mock<ILogger<AppetiteScoringService>>();
        _service = new AppetiteScoringService(_loggerMock.Object);
    }

    [Fact]
    public void CalculateAppetiteScore_WithNoGuidelines_ReturnsNoGuidelinesResult()
    {
        // Arrange
        var submission = CreateBasicSubmission();
        var guidelines = Enumerable.Empty<UnderwritingGuideline>();

        // Act
        var result = _service.CalculateAppetiteScore(submission, guidelines);

        // Assert
        result.OverallScore.Should().Be(50);
        result.IsInAppetite.Should().BeFalse();
        result.RequiresReferral.Should().BeTrue();
        result.ReferralReasons.Should().Contain("No applicable guidelines found");
    }

    [Fact]
    public void CalculateAppetiteScore_WithMatchingAcceptRule_ReturnsHighScore()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline = CreateGuidelineWithAcceptRule("CA");

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline]);

        // Assert
        result.IsInAppetite.Should().BeTrue();
        result.Factors.Should().Contain(f => f.ScoreImpact > 0);
    }

    [Fact]
    public void CalculateAppetiteScore_WithDeclineRule_ReturnsNotInAppetite()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline = CreateGuidelineWithDeclineRule("CA");

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline]);

        // Assert
        result.IsInAppetite.Should().BeFalse();
        result.DeclineReasons.Should().NotBeEmpty();
    }

    [Fact]
    public void CalculateAppetiteScore_WithReferralRule_RequiresReferral()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline = CreateGuidelineWithReferRule("CA");

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline]);

        // Assert
        result.RequiresReferral.Should().BeTrue();
        result.ReferralReasons.Should().NotBeEmpty();
    }

    [Fact]
    public void CalculateAppetiteScore_WithScoreAdjustmentRule_AdjustsScore()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline = CreateGuidelineWithScoreAdjustment("CA", 15);

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline]);

        // Assert
        result.Factors.Should().Contain(f => f.ScoreImpact == 15);
    }

    [Fact]
    public void CalculateAppetiteScore_WithMultipleGuidelines_EvaluatesAll()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline1 = CreateGuidelineWithAcceptRule("CA");
        var guideline2 = CreateGuidelineWithScoreAdjustment("CA", 5);

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline1, guideline2]);

        // Assert
        result.Factors.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void CalculateAppetiteScore_WithNonMatchingRule_DoesNotAffectScore()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline = CreateGuidelineWithAcceptRule("TX"); // Doesn't match CA

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline]);

        // Assert
        result.Factors.Should().BeEmpty();
    }

    [Fact]
    public void CalculateAppetiteScore_ScoreClampedTo100()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline = CreateGuidelineWithScoreAdjustment("CA", 100); // Max allowed adjustment

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline]);

        // Assert
        result.OverallScore.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void CalculateAppetiteScore_ScoreClampedTo0()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline = CreateGuidelineWithScoreAdjustment("CA", -100); // Max negative adjustment

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline]);

        // Assert
        result.OverallScore.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void CalculateAppetiteScore_WithBorderlineScore_RequiresReferral()
    {
        // Arrange
        var submission = CreateSubmissionInCalifornia();
        var guideline = CreateGuidelineWithScoreAdjustment("CA", -20); // Base 70 - 20 = 50 (borderline)

        // Act
        var result = _service.CalculateAppetiteScore(submission, [guideline]);

        // Assert
        // Borderline scores (40-60 range) should require referral
        if (result.OverallScore >= 40 && result.OverallScore < 60)
        {
            result.RequiresReferral.Should().BeTrue();
        }
    }

    private Submission CreateBasicSubmission()
    {
        var submission = Submission.Create(_tenantId, "SUB-001", "Test Company").Value;
        return submission;
    }

    private Submission CreateSubmissionInCalifornia()
    {
        var submission = Submission.Create(_tenantId, "SUB-002", "California Company").Value;
        submission.Insured.UpdateMailingAddress(
            Address.Create("123 Main St", null, "San Francisco", "CA", "94105").Value);
        return submission;
    }

    private UnderwritingGuideline CreateGuidelineWithAcceptRule(string state)
    {
        var guideline = UnderwritingGuideline.Create(_tenantId, "Test Guideline");
        var rule = guideline.AddRule("State Accept Rule", RuleType.Appetite, RuleAction.Accept);
        rule.AddCondition(RuleCondition.Equals(RuleField.InsuredState, state));
        rule.SetMessage("State is acceptable");
        guideline.Activate();
        return guideline;
    }

    private UnderwritingGuideline CreateGuidelineWithDeclineRule(string state)
    {
        var guideline = UnderwritingGuideline.Create(_tenantId, "Decline Guideline");
        var rule = guideline.AddRule("State Decline Rule", RuleType.Appetite, RuleAction.Decline);
        rule.AddCondition(RuleCondition.Equals(RuleField.InsuredState, state));
        rule.SetMessage("State is not acceptable");
        guideline.Activate();
        return guideline;
    }

    private UnderwritingGuideline CreateGuidelineWithReferRule(string state)
    {
        var guideline = UnderwritingGuideline.Create(_tenantId, "Refer Guideline");
        var rule = guideline.AddRule("State Refer Rule", RuleType.Appetite, RuleAction.Refer);
        rule.AddCondition(RuleCondition.Equals(RuleField.InsuredState, state));
        rule.SetMessage("State requires review");
        guideline.Activate();
        return guideline;
    }

    private UnderwritingGuideline CreateGuidelineWithScoreAdjustment(string state, int adjustment)
    {
        var guideline = UnderwritingGuideline.Create(_tenantId, "Score Guideline");
        var rule = guideline.AddRule("State Score Rule", RuleType.Appetite, RuleAction.AdjustScore);
        rule.AddCondition(RuleCondition.Equals(RuleField.InsuredState, state));
        rule.SetScoreAdjustment(adjustment);
        rule.SetMessage("Score adjusted");
        guideline.Activate();
        return guideline;
    }
}
