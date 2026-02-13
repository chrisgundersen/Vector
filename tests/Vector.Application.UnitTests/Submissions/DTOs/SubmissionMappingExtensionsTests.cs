using Vector.Application.Submissions.DTOs;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Application.UnitTests.Submissions.DTOs;

public class SubmissionMappingExtensionsTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private Submission CreateFullSubmission()
    {
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Set up insured party
        submission.Insured.UpdateDbaName("DBA Name");
        submission.Insured.UpdateFein("12-3456789");
        submission.Insured.UpdateMailingAddress(
            Address.Create("123 Main St", "Suite 100", "Austin", "TX", "78701").Value);
        submission.Insured.UpdateIndustry(
            IndustryClassification.Create("332618", "5051", "Metal Stamping").Value);
        submission.Insured.UpdateWebsite("https://example.com");
        submission.Insured.UpdateYearsInBusiness(25);
        submission.Insured.UpdateEmployeeCount(150);
        submission.Insured.UpdateAnnualRevenue(Money.FromDecimal(5000000));

        // Set dates
        submission.UpdatePolicyDates(new DateTime(2024, 4, 1), new DateTime(2025, 4, 1));
        submission.UpdateProducerInfo(null, "Test Producer", null);

        // Mark as received first, then assign underwriter
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Smith");

        // Set scores
        submission.UpdateScores(85, 70, 90);

        // Add coverage
        var coverage = submission.AddCoverage(CoverageType.GeneralLiability);
        coverage.UpdateRequestedLimit(Money.FromDecimal(1000000));
        coverage.UpdateRequestedDeductible(Money.FromDecimal(10000));
        coverage.UpdateEffectiveDates(new DateTime(2024, 4, 1), new DateTime(2025, 4, 1));
        coverage.UpdateCurrentInsurance(true, "Previous Carrier", Money.FromDecimal(15000));

        // Add location
        var location = submission.AddLocation(
            Address.Create("456 Industrial Blvd", null, "Dallas", "TX", "75201").Value);
        location.UpdateBuildingDescription("Office building");
        location.UpdateOccupancyType("Commercial");
        location.UpdateConstruction("Masonry", 1990, 3);
        location.UpdateSquareFootage(50000);
        location.UpdateValues(
            Money.FromDecimal(2000000),
            Money.FromDecimal(500000),
            Money.FromDecimal(250000));

        // Add loss
        submission.AddLoss(new DateTime(2023, 6, 15), "Water damage claim");

        return submission;
    }

    [Fact]
    public void ToDto_MapsSubmissionCorrectly()
    {
        // Arrange
        var submission = CreateFullSubmission();

        // Act
        var dto = submission.ToDto();

        // Assert
        dto.Id.Should().Be(submission.Id);
        dto.TenantId.Should().Be(_tenantId);
        dto.SubmissionNumber.Should().Be("SUB-2024-000001");
        dto.Status.Should().Be("InReview");
        dto.ReceivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dto.EffectiveDate.Should().Be(new DateTime(2024, 4, 1));
        dto.ExpirationDate.Should().Be(new DateTime(2025, 4, 1));
        dto.ProducerName.Should().Be("Test Producer");
        dto.AssignedUnderwriterName.Should().Be("John Smith");
        dto.AssignedAt.Should().NotBeNull();
        dto.AppetiteScore.Should().Be(85);
        dto.WinnabilityScore.Should().Be(70);
        dto.DataQualityScore.Should().Be(90);
        dto.Coverages.Should().HaveCount(1);
        dto.Locations.Should().HaveCount(1);
        dto.Losses.Should().HaveCount(1);
    }

    [Fact]
    public void ToDto_MapsInsuredPartyCorrectly()
    {
        // Arrange
        var submission = CreateFullSubmission();

        // Act
        var dto = submission.ToDto();

        // Assert
        dto.Insured.Should().NotBeNull();
        dto.Insured.Name.Should().Be("Test Insured");
        dto.Insured.DbaName.Should().Be("DBA Name");
        dto.Insured.FeinNumber.Should().Be("123456789");
        dto.Insured.MailingAddress.Should().NotBeNull();
        dto.Insured.MailingAddress!.Street1.Should().Be("123 Main St");
        dto.Insured.MailingAddress.Street2.Should().Be("Suite 100");
        dto.Insured.MailingAddress.City.Should().Be("Austin");
        dto.Insured.MailingAddress.State.Should().Be("TX");
        dto.Insured.MailingAddress.PostalCode.Should().Be("78701");
        dto.Insured.Industry.Should().Contain("332618");
        dto.Insured.Website.Should().Be("https://example.com");
        dto.Insured.YearsInBusiness.Should().Be(25);
        dto.Insured.EmployeeCount.Should().Be(150);
        dto.Insured.AnnualRevenue.Should().Be(5000000);
    }

    [Fact]
    public void ToDto_MapsInsuredPartyWithNullValues()
    {
        // Arrange
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Act
        var dto = submission.ToDto();

        // Assert
        dto.Insured.DbaName.Should().BeNull();
        dto.Insured.FeinNumber.Should().BeNull();
        dto.Insured.MailingAddress.Should().BeNull();
        dto.Insured.Industry.Should().BeNull();
        dto.Insured.Website.Should().BeNull();
        dto.Insured.YearsInBusiness.Should().BeNull();
        dto.Insured.EmployeeCount.Should().BeNull();
        dto.Insured.AnnualRevenue.Should().BeNull();
    }

    [Fact]
    public void ToDto_MapsCoverageCorrectly()
    {
        // Arrange
        var submission = CreateFullSubmission();

        // Act
        var dto = submission.ToDto();
        var coverageDto = dto.Coverages.First();

        // Assert
        coverageDto.Type.Should().Be("GeneralLiability");
        coverageDto.RequestedLimit.Should().Be(1000000);
        coverageDto.RequestedDeductible.Should().Be(10000);
        coverageDto.EffectiveDate.Should().Be(new DateTime(2024, 4, 1));
        coverageDto.ExpirationDate.Should().Be(new DateTime(2025, 4, 1));
        coverageDto.IsCurrentlyInsured.Should().BeTrue();
        coverageDto.CurrentCarrier.Should().Be("Previous Carrier");
        coverageDto.CurrentPremium.Should().Be(15000);
    }

    [Fact]
    public void ToDto_MapsCoverageWithNullValues()
    {
        // Arrange
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.AddCoverage(CoverageType.PropertyDamage);

        // Act
        var dto = submission.ToDto();
        var coverageDto = dto.Coverages.First();

        // Assert
        coverageDto.RequestedLimit.Should().BeNull();
        coverageDto.RequestedDeductible.Should().BeNull();
        coverageDto.EffectiveDate.Should().BeNull();
        coverageDto.ExpirationDate.Should().BeNull();
        coverageDto.IsCurrentlyInsured.Should().BeFalse();
        coverageDto.CurrentCarrier.Should().BeNull();
        coverageDto.CurrentPremium.Should().BeNull();
    }

    [Fact]
    public void ToDto_MapsLocationCorrectly()
    {
        // Arrange
        var submission = CreateFullSubmission();

        // Act
        var dto = submission.ToDto();
        var locationDto = dto.Locations.First();

        // Assert
        locationDto.LocationNumber.Should().Be(1);
        locationDto.Address.Street1.Should().Be("456 Industrial Blvd");
        locationDto.Address.City.Should().Be("Dallas");
        locationDto.Address.State.Should().Be("TX");
        locationDto.BuildingDescription.Should().Be("Office building");
        locationDto.OccupancyType.Should().Be("Commercial");
        locationDto.ConstructionType.Should().Be("Masonry");
        locationDto.YearBuilt.Should().Be(1990);
        locationDto.SquareFootage.Should().Be(50000);
        locationDto.BuildingValue.Should().Be(2000000);
        locationDto.ContentsValue.Should().Be(500000);
        locationDto.BusinessIncomeValue.Should().Be(250000);
        locationDto.TotalInsuredValue.Should().Be(2750000);
    }

    [Fact]
    public void ToDto_MapsLocationWithNullValues()
    {
        // Arrange
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        submission.AddLocation(Address.Create("123 Main St", null, "Austin", "TX", "78701").Value);

        // Act
        var dto = submission.ToDto();
        var locationDto = dto.Locations.First();

        // Assert
        locationDto.BuildingDescription.Should().BeNull();
        locationDto.OccupancyType.Should().BeNull();
        locationDto.ConstructionType.Should().BeNull();
        locationDto.YearBuilt.Should().BeNull();
        locationDto.SquareFootage.Should().BeNull();
        locationDto.BuildingValue.Should().BeNull();
        locationDto.ContentsValue.Should().BeNull();
        locationDto.BusinessIncomeValue.Should().BeNull();
        locationDto.TotalInsuredValue.Should().Be(0);
    }

    [Fact]
    public void ToDto_MapsSubmissionWithNullOptionalFields()
    {
        // Arrange
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;

        // Act
        var dto = submission.ToDto();

        // Assert
        dto.EffectiveDate.Should().BeNull();
        dto.ExpirationDate.Should().BeNull();
        dto.ProducerName.Should().BeNull();
        dto.AssignedUnderwriterName.Should().BeNull();
        dto.AssignedAt.Should().BeNull();
        dto.AppetiteScore.Should().BeNull();
        dto.WinnabilityScore.Should().BeNull();
        dto.DataQualityScore.Should().BeNull();
        dto.Coverages.Should().BeEmpty();
        dto.Locations.Should().BeEmpty();
        dto.Losses.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_MapsAddressWithAllFields()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Suite 100", "Austin", "TX", "78701").Value;

        // Act
        var dto = address.ToDto();

        // Assert
        dto.Street1.Should().Be("123 Main St");
        dto.Street2.Should().Be("Suite 100");
        dto.City.Should().Be("Austin");
        dto.State.Should().Be("TX");
        dto.PostalCode.Should().Be("78701");
        dto.Country.Should().Be("USA");
    }

    [Fact]
    public void ToDto_MapsAddressWithNullStreet2()
    {
        // Arrange
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701").Value;

        // Act
        var dto = address.ToDto();

        // Assert
        dto.Street1.Should().Be("123 Main St");
        dto.Street2.Should().BeNull();
    }

    [Fact]
    public void ToDto_MapsLossHistoryCorrectly()
    {
        // Arrange
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        var loss = submission.AddLoss(new DateTime(2023, 6, 15), "Water damage from burst pipe");
        loss.UpdateClaimInfo("CLM-2023-001", CoverageType.PropertyDamage, "ABC Insurance");
        loss.UpdateAmounts(
            Money.FromDecimal(5000),
            Money.FromDecimal(3000),
            Money.FromDecimal(10000));
        loss.UpdateStatus(LossStatus.ClosedWithPayment);

        // Act
        var dto = loss.ToDto();

        // Assert
        dto.Id.Should().Be(loss.Id);
        dto.DateOfLoss.Should().Be(new DateTime(2023, 6, 15));
        dto.Description.Should().Be("Water damage from burst pipe");
        dto.ClaimNumber.Should().Be("CLM-2023-001");
        dto.CoverageType.Should().Be("PropertyDamage");
        dto.Carrier.Should().Be("ABC Insurance");
        dto.PaidAmount.Should().Be(5000);
        dto.ReservedAmount.Should().Be(3000);
        dto.IncurredAmount.Should().Be(10000);
        dto.Status.Should().Be("ClosedWithPayment");
    }

    [Fact]
    public void ToDto_MapsLossHistoryWithNullValues()
    {
        // Arrange
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Insured").Value;
        var loss = submission.AddLoss(new DateTime(2023, 6, 15), "Test loss");
        // Don't set any optional values

        // Act
        var dto = loss.ToDto();

        // Assert
        dto.ClaimNumber.Should().BeNull();
        dto.CoverageType.Should().BeNull();
        dto.Carrier.Should().BeNull();
        dto.PaidAmount.Should().BeNull();
        dto.ReservedAmount.Should().BeNull();
        dto.IncurredAmount.Should().BeNull();
        dto.Status.Should().Be("Open");
    }
}
