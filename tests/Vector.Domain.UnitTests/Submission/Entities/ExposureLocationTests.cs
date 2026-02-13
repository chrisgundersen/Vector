using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.UnitTests.Submission.Entities;

public class ExposureLocationTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private ExposureLocation CreateLocation()
    {
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured");
        var submission = submissionResult.Value;
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701").Value;
        return submission.AddLocation(address);
    }

    [Fact]
    public void AddLocation_CreatesLocationWithCorrectNumber()
    {
        // Arrange
        var submissionResult = Domain.Submission.Aggregates.Submission.Create(
            _tenantId, "SUB-2024-000001", "Test Insured");
        var submission = submissionResult.Value;
        var address = Address.Create("123 Main St", null, "Austin", "TX", "78701").Value;

        // Act
        var location1 = submission.AddLocation(address);
        var location2 = submission.AddLocation(address);
        var location3 = submission.AddLocation(address);

        // Assert
        location1.LocationNumber.Should().Be(1);
        location2.LocationNumber.Should().Be(2);
        location3.LocationNumber.Should().Be(3);
    }

    [Fact]
    public void AddLocation_SetsAddressCorrectly()
    {
        // Act
        var location = CreateLocation();

        // Assert
        location.Address.Should().NotBeNull();
        location.Address.Street1.Should().Be("123 Main St");
        location.Address.City.Should().Be("Austin");
        location.Address.State.Should().Be("TX");
    }

    [Fact]
    public void UpdateBuildingDescription_SetsDescription()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateBuildingDescription("Three-story office building");

        // Assert
        location.BuildingDescription.Should().Be("Three-story office building");
    }

    [Fact]
    public void UpdateBuildingDescription_TrimsWhitespace()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateBuildingDescription("  Office building  ");

        // Assert
        location.BuildingDescription.Should().Be("Office building");
    }

    [Fact]
    public void UpdateOccupancyType_SetsType()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateOccupancyType("Office");

        // Assert
        location.OccupancyType.Should().Be("Office");
    }

    [Fact]
    public void UpdateConstruction_SetsAllValues()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateConstruction("Masonry", 1985, 3);

        // Assert
        location.ConstructionType.Should().Be("Masonry");
        location.YearBuilt.Should().Be(1985);
        location.NumberOfStories.Should().Be(3);
    }

    [Fact]
    public void UpdateConstruction_IgnoresInvalidYearBuilt()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateConstruction("Masonry", 1700, 3); // Before 1800

        // Assert
        location.ConstructionType.Should().Be("Masonry");
        location.YearBuilt.Should().BeNull();
    }

    [Fact]
    public void UpdateConstruction_IgnoresFutureYear()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateConstruction("Masonry", 2200, 3); // After 2100

        // Assert
        location.YearBuilt.Should().BeNull();
    }

    [Fact]
    public void UpdateConstruction_IgnoresZeroOrNegativeStories()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateConstruction("Masonry", 1985, 0);

        // Assert
        location.NumberOfStories.Should().BeNull();
    }

    [Fact]
    public void UpdateSquareFootage_SetsValue()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateSquareFootage(50000);

        // Assert
        location.SquareFootage.Should().Be(50000);
    }

    [Fact]
    public void UpdateSquareFootage_IgnoresZeroOrNegative()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateSquareFootage(0);
        location.UpdateSquareFootage(-100);

        // Assert
        location.SquareFootage.Should().BeNull();
    }

    [Fact]
    public void UpdateValues_SetsAllValues()
    {
        // Arrange
        var location = CreateLocation();
        var buildingValue = Money.FromDecimal(2000000);
        var contentsValue = Money.FromDecimal(500000);
        var biValue = Money.FromDecimal(250000);

        // Act
        location.UpdateValues(buildingValue, contentsValue, biValue);

        // Assert
        location.BuildingValue!.Amount.Should().Be(2000000);
        location.ContentsValue!.Amount.Should().Be(500000);
        location.BusinessIncomeValue!.Amount.Should().Be(250000);
    }

    [Fact]
    public void UpdateValues_AcceptsNullValues()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateValues(Money.FromDecimal(1000000), null, null);

        // Assert
        location.BuildingValue!.Amount.Should().Be(1000000);
        location.ContentsValue.Should().BeNull();
        location.BusinessIncomeValue.Should().BeNull();
    }

    [Fact]
    public void UpdateProtection_SetsAllFlags()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateProtection(true, true, false, "3");

        // Assert
        location.HasSprinklers.Should().BeTrue();
        location.HasFireAlarm.Should().BeTrue();
        location.HasSecuritySystem.Should().BeFalse();
        location.ProtectionClass.Should().Be("3");
    }

    [Fact]
    public void UpdateProtection_TrimsProtectionClass()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        location.UpdateProtection(false, false, false, "  5  ");

        // Assert
        location.ProtectionClass.Should().Be("5");
    }

    [Fact]
    public void TotalInsuredValue_SumsAllValues()
    {
        // Arrange
        var location = CreateLocation();
        location.UpdateValues(
            Money.FromDecimal(2000000),
            Money.FromDecimal(500000),
            Money.FromDecimal(250000));

        // Act
        var total = location.TotalInsuredValue;

        // Assert
        total.Amount.Should().Be(2750000);
    }

    [Fact]
    public void TotalInsuredValue_WithPartialValues_SumsCorrectly()
    {
        // Arrange
        var location = CreateLocation();
        location.UpdateValues(
            Money.FromDecimal(2000000),
            null,
            Money.FromDecimal(250000));

        // Act
        var total = location.TotalInsuredValue;

        // Assert
        total.Amount.Should().Be(2250000);
    }

    [Fact]
    public void TotalInsuredValue_WithNoValues_ReturnsZero()
    {
        // Arrange
        var location = CreateLocation();

        // Act
        var total = location.TotalInsuredValue;

        // Assert
        total.Amount.Should().Be(0);
    }
}
