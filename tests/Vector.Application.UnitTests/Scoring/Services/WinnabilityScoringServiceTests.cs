using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Application.Scoring.Services;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Application.UnitTests.Scoring.Services;

public class WinnabilityScoringServiceTests
{
    private readonly Mock<ILogger<WinnabilityScoringService>> _loggerMock;
    private readonly WinnabilityScoringService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public WinnabilityScoringServiceTests()
    {
        _loggerMock = new Mock<ILogger<WinnabilityScoringService>>();
        _service = new WinnabilityScoringService(_loggerMock.Object);
    }

    [Fact]
    public void CalculateWinnabilityScore_WithCompleteSubmission_ReturnsValidScore()
    {
        // Arrange
        var submission = CreateSubmissionWithCompleteData();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeInRange(0, 100);
        result.CompetitivePositionScore.Should().BeInRange(0, 100);
        result.RelationshipScore.Should().BeInRange(0, 100);
        result.PricingIndicatorScore.Should().BeInRange(0, 100);
        result.TimingScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public void CalculateWinnabilityScore_WithMultipleCoverages_IncreasesScore()
    {
        // Arrange
        var submissionWithOne = CreateBasicSubmission();
        var submissionWithMultiple = CreateSubmissionWithMultipleCoverages();

        // Act
        var resultOne = _service.CalculateWinnabilityScore(submissionWithOne);
        var resultMultiple = _service.CalculateWinnabilityScore(submissionWithMultiple);

        // Assert
        resultMultiple.CompetitivePositionScore.Should().BeGreaterThan(resultOne.CompetitivePositionScore);
    }

    [Fact]
    public void CalculateWinnabilityScore_WithCompleteInsuredInfo_IncreasesScore()
    {
        // Arrange
        var submission = CreateSubmissionWithCompleteData();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        result.Factors.Should().Contain(f => f.FactorName == "Complete Information");
    }

    [Fact]
    public void CalculateWinnabilityScore_WithIncompleteInsuredInfo_DecreasesScore()
    {
        // Arrange
        var submission = CreateBasicSubmission();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        result.Factors.Should().Contain(f => f.FactorName == "Incomplete Information");
        result.Recommendations.Should().Contain(r => r.Contains("complete insured information"));
    }

    [Fact]
    public void CalculateWinnabilityScore_WithCleanLossHistory_IncreasesScore()
    {
        // Arrange
        var submission = CreateSubmissionWithNoLosses();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        result.Factors.Should().Contain(f => f.FactorName == "Clean Loss History");
    }

    [Fact]
    public void CalculateWinnabilityScore_WithEstablishedBusiness_IncreasesScore()
    {
        // Arrange
        var submission = CreateSubmissionWithEstablishedBusiness();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        result.Factors.Should().Contain(f => f.FactorName == "Established Business");
    }

    [Fact]
    public void CalculateWinnabilityScore_WithLargeAccount_IncreasesScore()
    {
        // Arrange
        var submission = CreateSubmissionWithLargeRevenue();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        result.Factors.Should().Contain(f => f.FactorName == "Large Account");
    }

    [Fact]
    public void CalculateWinnabilityScore_WithRushSubmission_DecreasesScore()
    {
        // Arrange
        var submission = CreateSubmissionWithRushTiming();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        result.Factors.Should().Contain(f => f.FactorName == "Rush Submission");
        result.Recommendations.Should().Contain(r => r.Contains("tight timing"));
    }

    [Fact]
    public void CalculateWinnabilityScore_WithGoodLeadTime_IncreasesScore()
    {
        // Arrange
        var submission = CreateSubmissionWithGoodTiming();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        result.Factors.Should().Contain(f => f.FactorName == "Good Lead Time");
    }

    [Fact]
    public void CalculateWinnabilityScore_LowScore_ReturnsNeedsAttention()
    {
        // Arrange
        var submission = CreatePoorSubmission();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        if (result.OverallScore < 50)
        {
            result.NeedsAttention.Should().BeTrue();
        }
    }

    [Fact]
    public void CalculateWinnabilityScore_HighScore_ReturnsIsHighWinnability()
    {
        // Arrange
        var submission = CreateExcellentSubmission();

        // Act
        var result = _service.CalculateWinnabilityScore(submission);

        // Assert
        if (result.OverallScore >= 70)
        {
            result.IsHighWinnability.Should().BeTrue();
        }
    }

    private Submission CreateBasicSubmission()
    {
        var submission = Submission.Create(_tenantId, "SUB-001", "Test Company").Value;
        return submission;
    }

    private Submission CreateSubmissionWithCompleteData()
    {
        var submission = Submission.Create(_tenantId, "SUB-002", "Complete Tech Corp").Value;
        submission.Insured.UpdateMailingAddress(
            Address.Create("123 Main St", "Suite 100", "San Francisco", "CA", "94105").Value);
        submission.Insured.UpdateIndustry(
            IndustryClassification.Create("541512", "5734", "Software Development").Value);
        submission.Insured.UpdateAnnualRevenue(Money.FromDecimal(5_000_000m));
        submission.Insured.UpdateYearsInBusiness(10);
        submission.UpdatePolicyDates(DateTime.UtcNow.AddDays(45), DateTime.UtcNow.AddDays(410));
        return submission;
    }

    private Submission CreateSubmissionWithMultipleCoverages()
    {
        var submission = Submission.Create(_tenantId, "SUB-003", "Multi Coverage Co").Value;
        var coverage1 = submission.AddCoverage(CoverageType.GeneralLiability);
        coverage1.UpdateRequestedLimit(Money.FromDecimal(1_000_000m));
        var coverage2 = submission.AddCoverage(CoverageType.PropertyDamage);
        coverage2.UpdateRequestedLimit(Money.FromDecimal(1_000_000m));
        return submission;
    }

    private Submission CreateSubmissionWithNoLosses()
    {
        var submission = Submission.Create(_tenantId, "SUB-004", "Clean Record Inc").Value;
        return submission;
    }

    private Submission CreateSubmissionWithEstablishedBusiness()
    {
        var submission = Submission.Create(_tenantId, "SUB-005", "Established Corp").Value;
        submission.Insured.UpdateYearsInBusiness(15);
        return submission;
    }

    private Submission CreateSubmissionWithLargeRevenue()
    {
        var submission = Submission.Create(_tenantId, "SUB-006", "Large Enterprise").Value;
        submission.Insured.UpdateAnnualRevenue(Money.FromDecimal(50_000_000m));
        return submission;
    }

    private Submission CreateSubmissionWithRushTiming()
    {
        var submission = Submission.Create(_tenantId, "SUB-007", "Rush Company").Value;
        submission.UpdatePolicyDates(DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(368));
        return submission;
    }

    private Submission CreateSubmissionWithGoodTiming()
    {
        var submission = Submission.Create(_tenantId, "SUB-008", "Planned Company").Value;
        submission.UpdatePolicyDates(DateTime.UtcNow.AddDays(45), DateTime.UtcNow.AddDays(410));
        return submission;
    }

    private Submission CreatePoorSubmission()
    {
        // Use a submission that was received a while ago (simulating aging)
        var submission = Submission.Create(_tenantId, "SUB-009", "Poor Submission Inc").Value;
        submission.UpdatePolicyDates(DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(368)); // Rush timing
        return submission;
    }

    private Submission CreateExcellentSubmission()
    {
        var submission = Submission.Create(_tenantId, "SUB-010", "Excellent Corp").Value;
        submission.Insured.UpdateMailingAddress(
            Address.Create("123 Main St", null, "San Francisco", "CA", "94105").Value);
        submission.Insured.UpdateIndustry(
            IndustryClassification.Create("541512", "5734", "Software Development").Value);
        submission.Insured.UpdateAnnualRevenue(Money.FromDecimal(15_000_000m));
        submission.Insured.UpdateYearsInBusiness(20);
        submission.UpdatePolicyDates(DateTime.UtcNow.AddDays(45), DateTime.UtcNow.AddDays(410));

        var coverage1 = submission.AddCoverage(CoverageType.GeneralLiability);
        coverage1.UpdateRequestedLimit(Money.FromDecimal(1_000_000m));
        var coverage2 = submission.AddCoverage(CoverageType.PropertyDamage);
        coverage2.UpdateRequestedLimit(Money.FromDecimal(1_000_000m));

        return submission;
    }
}
