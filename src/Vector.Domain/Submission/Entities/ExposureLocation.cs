using Vector.Domain.Common;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.Submission.Entities;

/// <summary>
/// Entity representing a location with exposure (property, operations, etc.).
/// </summary>
public sealed class ExposureLocation : Entity
{
    public int LocationNumber { get; private set; }
    public Address Address { get; private set; } = null!;
    public string? BuildingDescription { get; private set; }
    public string? OccupancyType { get; private set; }
    public string? ConstructionType { get; private set; }
    public int? YearBuilt { get; private set; }
    public int? SquareFootage { get; private set; }
    public int? NumberOfStories { get; private set; }
    public Money? BuildingValue { get; private set; }
    public Money? ContentsValue { get; private set; }
    public Money? BusinessIncomeValue { get; private set; }
    public bool HasSprinklers { get; private set; }
    public bool HasFireAlarm { get; private set; }
    public bool HasSecuritySystem { get; private set; }
    public string? ProtectionClass { get; private set; }

    private ExposureLocation()
    {
    }

    internal ExposureLocation(Guid id, int locationNumber, Address address) : base(id)
    {
        LocationNumber = locationNumber;
        Address = address;
    }

    public void UpdateBuildingDescription(string? description)
    {
        BuildingDescription = description?.Trim();
    }

    public void UpdateOccupancyType(string? occupancyType)
    {
        OccupancyType = occupancyType?.Trim();
    }

    public void UpdateConstruction(string? constructionType, int? yearBuilt, int? stories)
    {
        ConstructionType = constructionType?.Trim();
        if (yearBuilt is > 1800 and <= 2100)
        {
            YearBuilt = yearBuilt;
        }
        if (stories is > 0)
        {
            NumberOfStories = stories;
        }
    }

    public void UpdateSquareFootage(int? squareFootage)
    {
        if (squareFootage is > 0)
        {
            SquareFootage = squareFootage;
        }
    }

    public void UpdateValues(Money? buildingValue, Money? contentsValue, Money? businessIncomeValue)
    {
        BuildingValue = buildingValue;
        ContentsValue = contentsValue;
        BusinessIncomeValue = businessIncomeValue;
    }

    public void UpdateProtection(bool hasSprinklers, bool hasFireAlarm, bool hasSecuritySystem, string? protectionClass)
    {
        HasSprinklers = hasSprinklers;
        HasFireAlarm = hasFireAlarm;
        HasSecuritySystem = hasSecuritySystem;
        ProtectionClass = protectionClass?.Trim();
    }

    public Money TotalInsuredValue
    {
        get
        {
            var total = Money.Zero();
            if (BuildingValue is not null) total = total.Add(BuildingValue);
            if (ContentsValue is not null) total = total.Add(ContentsValue);
            if (BusinessIncomeValue is not null) total = total.Add(BusinessIncomeValue);
            return total;
        }
    }
}
