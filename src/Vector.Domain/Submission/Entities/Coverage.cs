using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.Submission.Entities;

/// <summary>
/// Entity representing a coverage requested in a submission.
/// </summary>
public sealed class Coverage : Entity
{
    public CoverageType Type { get; private set; }
    public Money? RequestedLimit { get; private set; }
    public Money? RequestedDeductible { get; private set; }
    public DateTime? EffectiveDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public bool IsCurrentlyInsured { get; private set; }
    public string? CurrentCarrier { get; private set; }
    public Money? CurrentPremium { get; private set; }
    public string? AdditionalInfo { get; private set; }

    private Coverage()
    {
    }

    internal Coverage(Guid id, CoverageType type) : base(id)
    {
        Type = type;
    }

    public void UpdateRequestedLimit(Money? limit)
    {
        RequestedLimit = limit;
    }

    public void UpdateRequestedDeductible(Money? deductible)
    {
        RequestedDeductible = deductible;
    }

    public void UpdateEffectiveDates(DateTime? effectiveDate, DateTime? expirationDate)
    {
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
    }

    public void UpdateCurrentInsurance(bool isInsured, string? carrier, Money? premium)
    {
        IsCurrentlyInsured = isInsured;
        CurrentCarrier = carrier?.Trim();
        CurrentPremium = premium;
    }

    public void UpdateAdditionalInfo(string? info)
    {
        AdditionalInfo = info?.Trim();
    }

    public int? PolicyTermDays => EffectiveDate.HasValue && ExpirationDate.HasValue
        ? (ExpirationDate.Value - EffectiveDate.Value).Days
        : null;
}
