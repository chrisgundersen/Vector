using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.UnitTests.Submission.Entities;

public class InsuredPartyTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private InsuredParty CreateInsuredParty(string name = "Test Company")
    {
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", name);
        return submissionResult.Value.Insured;
    }

    [Fact]
    public void CreateSubmission_CreatesInsuredPartyWithName()
    {
        // Act
        var insuredParty = CreateInsuredParty("ABC Manufacturing");

        // Assert
        insuredParty.Should().NotBeNull();
        insuredParty.Name.Should().Be("ABC Manufacturing");
    }

    [Fact]
    public void UpdateName_WithValidName_UpdatesName()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateName("New Company Name");

        // Assert
        insuredParty.Name.Should().Be("New Company Name");
    }

    [Fact]
    public void UpdateName_WithWhitespace_TrimsName()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateName("  Trimmed Name  ");

        // Assert
        insuredParty.Name.Should().Be("Trimmed Name");
    }

    [Fact]
    public void UpdateName_WithNullOrEmpty_DoesNotUpdate()
    {
        // Arrange
        var insuredParty = CreateInsuredParty("Original Name");

        // Act
        insuredParty.UpdateName("");
        insuredParty.UpdateName("   ");

        // Assert
        insuredParty.Name.Should().Be("Original Name");
    }

    [Fact]
    public void UpdateDbaName_SetsDbaName()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateDbaName("DBA Name Inc");

        // Assert
        insuredParty.DbaName.Should().Be("DBA Name Inc");
    }

    [Fact]
    public void UpdateDbaName_TrimsWhitespace()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateDbaName("  DBA Name  ");

        // Assert
        insuredParty.DbaName.Should().Be("DBA Name");
    }

    [Fact]
    public void UpdateDbaName_WithNull_SetsNull()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateDbaName("Initial DBA");

        // Act
        insuredParty.UpdateDbaName(null);

        // Assert
        insuredParty.DbaName.Should().BeNull();
    }

    [Fact]
    public void UpdateFein_SetsFeinNumber()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateFein("123456789");

        // Assert
        insuredParty.FeinNumber.Should().Be("123456789");
    }

    [Fact]
    public void UpdateFein_RemovesDashes()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateFein("12-3456789");

        // Assert
        insuredParty.FeinNumber.Should().Be("123456789");
    }

    [Fact]
    public void UpdateFein_TrimsWhitespace()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateFein("  12-3456789  ");

        // Assert
        insuredParty.FeinNumber.Should().Be("123456789");
    }

    [Fact]
    public void UpdateFein_WithNull_SetsNull()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateFein("123456789");

        // Act
        insuredParty.UpdateFein(null);

        // Assert
        insuredParty.FeinNumber.Should().BeNull();
    }

    [Fact]
    public void UpdateMailingAddress_SetsAddress()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701").Value;

        // Act
        insuredParty.UpdateMailingAddress(address);

        // Assert
        insuredParty.MailingAddress.Should().NotBeNull();
        insuredParty.MailingAddress!.Street1.Should().Be("123 Main St");
        insuredParty.MailingAddress.City.Should().Be("Austin");
    }

    [Fact]
    public void UpdateIndustry_SetsIndustryClassification()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        var industry = IndustryClassification.Create("332618", "5051", "Metal Stamping").Value;

        // Act
        insuredParty.UpdateIndustry(industry);

        // Assert
        insuredParty.Industry.Should().NotBeNull();
        insuredParty.Industry!.NaicsCode.Should().Be("332618");
        insuredParty.Industry.Description.Should().Be("Metal Stamping");
    }

    [Fact]
    public void UpdateWebsite_SetsWebsite()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateWebsite("https://example.com");

        // Assert
        insuredParty.Website.Should().Be("https://example.com");
    }

    [Fact]
    public void UpdateWebsite_TrimsWhitespace()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateWebsite("  https://example.com  ");

        // Assert
        insuredParty.Website.Should().Be("https://example.com");
    }

    [Fact]
    public void UpdateWebsite_WithNull_SetsNull()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateWebsite("https://example.com");

        // Act
        insuredParty.UpdateWebsite(null);

        // Assert
        insuredParty.Website.Should().BeNull();
    }

    [Fact]
    public void UpdateYearsInBusiness_WithValidValue_SetsYears()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateYearsInBusiness(25);

        // Assert
        insuredParty.YearsInBusiness.Should().Be(25);
    }

    [Fact]
    public void UpdateYearsInBusiness_WithZero_SetsZero()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateYearsInBusiness(0);

        // Assert
        insuredParty.YearsInBusiness.Should().Be(0);
    }

    [Fact]
    public void UpdateYearsInBusiness_WithNegative_DoesNotUpdate()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateYearsInBusiness(10);

        // Act
        insuredParty.UpdateYearsInBusiness(-5);

        // Assert
        insuredParty.YearsInBusiness.Should().Be(10);
    }

    [Fact]
    public void UpdateYearsInBusiness_WithNull_DoesNotUpdate()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateYearsInBusiness(10);

        // Act
        insuredParty.UpdateYearsInBusiness(null);

        // Assert
        insuredParty.YearsInBusiness.Should().Be(10);
    }

    [Fact]
    public void UpdateEmployeeCount_WithValidValue_SetsCount()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateEmployeeCount(150);

        // Assert
        insuredParty.EmployeeCount.Should().Be(150);
    }

    [Fact]
    public void UpdateEmployeeCount_WithZero_SetsZero()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateEmployeeCount(0);

        // Assert
        insuredParty.EmployeeCount.Should().Be(0);
    }

    [Fact]
    public void UpdateEmployeeCount_WithNegative_DoesNotUpdate()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateEmployeeCount(100);

        // Act
        insuredParty.UpdateEmployeeCount(-10);

        // Assert
        insuredParty.EmployeeCount.Should().Be(100);
    }

    [Fact]
    public void UpdateEmployeeCount_WithNull_DoesNotUpdate()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateEmployeeCount(100);

        // Act
        insuredParty.UpdateEmployeeCount(null);

        // Assert
        insuredParty.EmployeeCount.Should().Be(100);
    }

    [Fact]
    public void UpdateAnnualRevenue_SetsRevenue()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        var revenue = Money.FromDecimal(5000000);

        // Act
        insuredParty.UpdateAnnualRevenue(revenue);

        // Assert
        insuredParty.AnnualRevenue.Should().NotBeNull();
        insuredParty.AnnualRevenue!.Amount.Should().Be(5000000);
    }

    [Fact]
    public void UpdateAnnualRevenue_WithNull_SetsNull()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateAnnualRevenue(Money.FromDecimal(1000000));

        // Act
        insuredParty.UpdateAnnualRevenue(null);

        // Assert
        insuredParty.AnnualRevenue.Should().BeNull();
    }

    [Fact]
    public void UpdateEntityFormationDate_SetsDate()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        var formationDate = new DateTime(1995, 6, 15);

        // Act
        insuredParty.UpdateEntityFormationDate(formationDate);

        // Assert
        insuredParty.EntityFormationDate.Should().Be(formationDate);
    }

    [Fact]
    public void UpdateEntityFormationDate_WithNull_SetsNull()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateEntityFormationDate(new DateTime(2000, 1, 1));

        // Act
        insuredParty.UpdateEntityFormationDate(null);

        // Assert
        insuredParty.EntityFormationDate.Should().BeNull();
    }

    [Fact]
    public void UpdateEntityType_SetsEntityType()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateEntityType("LLC");

        // Assert
        insuredParty.EntityType.Should().Be("LLC");
    }

    [Fact]
    public void UpdateEntityType_TrimsWhitespace()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();

        // Act
        insuredParty.UpdateEntityType("  Corporation  ");

        // Assert
        insuredParty.EntityType.Should().Be("Corporation");
    }

    [Fact]
    public void UpdateEntityType_WithNull_SetsNull()
    {
        // Arrange
        var insuredParty = CreateInsuredParty();
        insuredParty.UpdateEntityType("LLC");

        // Act
        insuredParty.UpdateEntityType(null);

        // Assert
        insuredParty.EntityType.Should().BeNull();
    }
}
