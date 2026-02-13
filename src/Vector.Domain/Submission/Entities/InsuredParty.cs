using Vector.Domain.Common;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.Submission.Entities;

/// <summary>
/// Entity representing the insured party (applicant) on a submission.
/// </summary>
public sealed class InsuredParty : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? DbaName { get; private set; }
    public string? FeinNumber { get; private set; }
    public Address? MailingAddress { get; private set; }
    public IndustryClassification? Industry { get; private set; }
    public string? Website { get; private set; }
    public int? YearsInBusiness { get; private set; }
    public int? EmployeeCount { get; private set; }
    public Money? AnnualRevenue { get; private set; }
    public DateTime? EntityFormationDate { get; private set; }
    public string? EntityType { get; private set; }

    private InsuredParty()
    {
    }

    internal InsuredParty(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public void UpdateName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }
    }

    public void UpdateDbaName(string? dbaName)
    {
        DbaName = dbaName?.Trim();
    }

    public void UpdateFein(string? fein)
    {
        FeinNumber = fein?.Trim().Replace("-", "");
    }

    public void UpdateMailingAddress(Address address)
    {
        MailingAddress = address;
    }

    public void UpdateIndustry(IndustryClassification industry)
    {
        Industry = industry;
    }

    public void UpdateWebsite(string? website)
    {
        Website = website?.Trim();
    }

    public void UpdateYearsInBusiness(int? years)
    {
        if (years.HasValue && years.Value >= 0)
        {
            YearsInBusiness = years.Value;
        }
    }

    public void UpdateEmployeeCount(int? count)
    {
        if (count.HasValue && count.Value >= 0)
        {
            EmployeeCount = count.Value;
        }
    }

    public void UpdateAnnualRevenue(Money? revenue)
    {
        AnnualRevenue = revenue;
    }

    public void UpdateEntityFormationDate(DateTime? date)
    {
        EntityFormationDate = date;
    }

    public void UpdateEntityType(string? entityType)
    {
        EntityType = entityType?.Trim();
    }
}
