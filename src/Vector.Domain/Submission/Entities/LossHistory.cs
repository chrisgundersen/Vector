using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;

namespace Vector.Domain.Submission.Entities;

/// <summary>
/// Entity representing a historical loss/claim.
/// </summary>
public sealed class LossHistory : Entity
{
    public DateTime DateOfLoss { get; private set; }
    public CoverageType? CoverageType { get; private set; }
    public string? ClaimNumber { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Money? PaidAmount { get; private set; }
    public Money? ReservedAmount { get; private set; }
    public Money? IncurredAmount { get; private set; }
    public LossStatus Status { get; private set; }
    public string? Carrier { get; private set; }
    public bool IsSubrogation { get; private set; }

    private LossHistory()
    {
    }

    internal LossHistory(Guid id, DateTime dateOfLoss, string description) : base(id)
    {
        DateOfLoss = dateOfLoss;
        Description = description;
        Status = LossStatus.Open;
    }

    public void UpdateClaimInfo(string? claimNumber, CoverageType? coverageType, string? carrier)
    {
        ClaimNumber = claimNumber?.Trim();
        CoverageType = coverageType;
        Carrier = carrier?.Trim();
    }

    public void UpdateAmounts(Money? paid, Money? reserved, Money? incurred)
    {
        PaidAmount = paid;
        ReservedAmount = reserved;
        IncurredAmount = incurred ?? CalculateIncurred(paid, reserved);
    }

    public void UpdateStatus(LossStatus status)
    {
        Status = status;
    }

    public void MarkAsSubrogation(bool isSubrogation)
    {
        IsSubrogation = isSubrogation;
    }

    private static Money? CalculateIncurred(Money? paid, Money? reserved)
    {
        if (paid is null && reserved is null) return null;

        var total = Money.Zero();
        if (paid is not null) total = total.Add(paid);
        if (reserved is not null) total = total.Add(reserved);
        return total;
    }

    public Money TotalIncurred => IncurredAmount ?? CalculateIncurred(PaidAmount, ReservedAmount) ?? Money.Zero();
}

public enum LossStatus
{
    Open = 0,
    Closed = 1,
    ClosedWithPayment = 2,
    ClosedWithoutPayment = 3,
    Reopened = 4
}
