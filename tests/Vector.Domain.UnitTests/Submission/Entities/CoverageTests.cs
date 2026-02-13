using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.UnitTests.Submission.Entities;

public class CoverageTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private Coverage CreateCoverage(CoverageType type = CoverageType.GeneralLiability)
    {
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured");
        var submission = submissionResult.Value;
        return submission.AddCoverage(type);
    }

    [Fact]
    public void AddCoverage_CreatesCoverageWithCorrectType()
    {
        // Act
        var coverage = CreateCoverage(CoverageType.PropertyDamage);

        // Assert
        coverage.Should().NotBeNull();
        coverage.Id.Should().NotBeEmpty();
        coverage.Type.Should().Be(CoverageType.PropertyDamage);
    }

    [Fact]
    public void UpdateRequestedLimit_SetsLimit()
    {
        // Arrange
        var coverage = CreateCoverage();
        var limit = Money.FromDecimal(1000000);

        // Act
        coverage.UpdateRequestedLimit(limit);

        // Assert
        coverage.RequestedLimit.Should().NotBeNull();
        coverage.RequestedLimit!.Amount.Should().Be(1000000);
    }

    [Fact]
    public void UpdateRequestedLimit_WithNull_ClearsLimit()
    {
        // Arrange
        var coverage = CreateCoverage();
        coverage.UpdateRequestedLimit(Money.FromDecimal(1000000));

        // Act
        coverage.UpdateRequestedLimit(null);

        // Assert
        coverage.RequestedLimit.Should().BeNull();
    }

    [Fact]
    public void UpdateRequestedDeductible_SetsDeductible()
    {
        // Arrange
        var coverage = CreateCoverage();
        var deductible = Money.FromDecimal(25000);

        // Act
        coverage.UpdateRequestedDeductible(deductible);

        // Assert
        coverage.RequestedDeductible.Should().NotBeNull();
        coverage.RequestedDeductible!.Amount.Should().Be(25000);
    }

    [Fact]
    public void UpdateEffectiveDates_SetsDates()
    {
        // Arrange
        var coverage = CreateCoverage();
        var effectiveDate = new DateTime(2024, 4, 1);
        var expirationDate = new DateTime(2025, 4, 1);

        // Act
        coverage.UpdateEffectiveDates(effectiveDate, expirationDate);

        // Assert
        coverage.EffectiveDate.Should().Be(effectiveDate);
        coverage.ExpirationDate.Should().Be(expirationDate);
    }

    [Fact]
    public void UpdateCurrentInsurance_SetsInsuranceInfo()
    {
        // Arrange
        var coverage = CreateCoverage();
        var premium = Money.FromDecimal(15000);

        // Act
        coverage.UpdateCurrentInsurance(true, "Current Carrier Inc", premium);

        // Assert
        coverage.IsCurrentlyInsured.Should().BeTrue();
        coverage.CurrentCarrier.Should().Be("Current Carrier Inc");
        coverage.CurrentPremium.Should().NotBeNull();
        coverage.CurrentPremium!.Amount.Should().Be(15000);
    }

    [Fact]
    public void UpdateCurrentInsurance_TrimsCarrierName()
    {
        // Arrange
        var coverage = CreateCoverage();

        // Act
        coverage.UpdateCurrentInsurance(true, "  Current Carrier Inc  ", null);

        // Assert
        coverage.CurrentCarrier.Should().Be("Current Carrier Inc");
    }

    [Fact]
    public void UpdateCurrentInsurance_WithNotInsured_SetsFlag()
    {
        // Arrange
        var coverage = CreateCoverage();

        // Act
        coverage.UpdateCurrentInsurance(false, null, null);

        // Assert
        coverage.IsCurrentlyInsured.Should().BeFalse();
        coverage.CurrentCarrier.Should().BeNull();
        coverage.CurrentPremium.Should().BeNull();
    }

    [Fact]
    public void UpdateAdditionalInfo_SetsInfo()
    {
        // Arrange
        var coverage = CreateCoverage();

        // Act
        coverage.UpdateAdditionalInfo("Additional coverage notes");

        // Assert
        coverage.AdditionalInfo.Should().Be("Additional coverage notes");
    }

    [Fact]
    public void UpdateAdditionalInfo_TrimsWhitespace()
    {
        // Arrange
        var coverage = CreateCoverage();

        // Act
        coverage.UpdateAdditionalInfo("  Notes with whitespace  ");

        // Assert
        coverage.AdditionalInfo.Should().Be("Notes with whitespace");
    }

    [Fact]
    public void UpdateAdditionalInfo_WithNull_ClearsInfo()
    {
        // Arrange
        var coverage = CreateCoverage();
        coverage.UpdateAdditionalInfo("Some info");

        // Act
        coverage.UpdateAdditionalInfo(null);

        // Assert
        coverage.AdditionalInfo.Should().BeNull();
    }

    [Fact]
    public void PolicyTermDays_WithBothDates_CalculatesDays()
    {
        // Arrange
        var coverage = CreateCoverage();
        var effectiveDate = new DateTime(2024, 1, 1);
        var expirationDate = new DateTime(2025, 1, 1);
        coverage.UpdateEffectiveDates(effectiveDate, expirationDate);

        // Act
        var days = coverage.PolicyTermDays;

        // Assert
        days.Should().Be(366); // 2024 is a leap year
    }

    [Fact]
    public void PolicyTermDays_WithoutEffectiveDate_ReturnsNull()
    {
        // Arrange
        var coverage = CreateCoverage();
        coverage.UpdateEffectiveDates(null, new DateTime(2025, 1, 1));

        // Act
        var days = coverage.PolicyTermDays;

        // Assert
        days.Should().BeNull();
    }

    [Fact]
    public void PolicyTermDays_WithoutExpirationDate_ReturnsNull()
    {
        // Arrange
        var coverage = CreateCoverage();
        coverage.UpdateEffectiveDates(new DateTime(2024, 1, 1), null);

        // Act
        var days = coverage.PolicyTermDays;

        // Assert
        days.Should().BeNull();
    }

    [Fact]
    public void PolicyTermDays_WithNoDates_ReturnsNull()
    {
        // Arrange
        var coverage = CreateCoverage();

        // Act
        var days = coverage.PolicyTermDays;

        // Assert
        days.Should().BeNull();
    }

    [Theory]
    [InlineData(CoverageType.GeneralLiability)]
    [InlineData(CoverageType.PropertyDamage)]
    [InlineData(CoverageType.WorkersCompensation)]
    [InlineData(CoverageType.ProfessionalLiability)]
    [InlineData(CoverageType.Cyber)]
    public void AddCoverage_SupportsDifferentCoverageTypes(CoverageType coverageType)
    {
        // Act
        var coverage = CreateCoverage(coverageType);

        // Assert
        coverage.Type.Should().Be(coverageType);
    }
}
