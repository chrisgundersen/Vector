using Vector.Domain.Common;
using Vector.Domain.Submission.Enums;

namespace Vector.Domain.Routing.Entities;

/// <summary>
/// Entity representing a pairing between a producer and an underwriter.
/// Used for routing submissions from specific producers to designated underwriters.
/// </summary>
public sealed class ProducerUnderwriterPairing : Entity
{
    public Guid ProducerId { get; private set; }
    public string ProducerName { get; private set; } = string.Empty;
    public Guid UnderwriterId { get; private set; }
    public string UnderwriterName { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveUntil { get; private set; }

    private readonly List<CoverageType> _coverageTypes = [];
    public IReadOnlyCollection<CoverageType> CoverageTypes => _coverageTypes.AsReadOnly();

    private ProducerUnderwriterPairing()
    {
    }

    private ProducerUnderwriterPairing(
        Guid id,
        Guid producerId,
        string producerName,
        Guid underwriterId,
        string underwriterName) : base(id)
    {
        ProducerId = producerId;
        ProducerName = producerName;
        UnderwriterId = underwriterId;
        UnderwriterName = underwriterName;
        Priority = 100;
        IsActive = true;
        EffectiveFrom = DateTime.UtcNow;
    }

    public static ProducerUnderwriterPairing Create(
        Guid producerId,
        string producerName,
        Guid underwriterId,
        string underwriterName)
    {
        if (producerId == Guid.Empty)
            throw new ArgumentException("Producer ID cannot be empty.", nameof(producerId));

        ArgumentException.ThrowIfNullOrWhiteSpace(producerName, nameof(producerName));

        if (underwriterId == Guid.Empty)
            throw new ArgumentException("Underwriter ID cannot be empty.", nameof(underwriterId));

        ArgumentException.ThrowIfNullOrWhiteSpace(underwriterName, nameof(underwriterName));

        return new ProducerUnderwriterPairing(
            Guid.NewGuid(),
            producerId,
            producerName.Trim(),
            underwriterId,
            underwriterName.Trim());
    }

    public void SetPriority(int priority)
    {
        if (priority < 1 || priority > 1000)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 1 and 1000.");

        Priority = priority;
    }

    public void SetEffectivePeriod(DateTime from, DateTime? until = null)
    {
        if (until.HasValue && until.Value <= from)
            throw new ArgumentException("Effective until date must be after effective from date.");

        EffectiveFrom = from;
        EffectiveUntil = until;
    }

    public void AddCoverageType(CoverageType coverageType)
    {
        if (!_coverageTypes.Contains(coverageType))
        {
            _coverageTypes.Add(coverageType);
        }
    }

    public void RemoveCoverageType(CoverageType coverageType)
    {
        _coverageTypes.Remove(coverageType);
    }

    public void ClearCoverageTypes()
    {
        _coverageTypes.Clear();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public bool IsEffective(DateTime asOf)
    {
        return IsActive &&
               asOf >= EffectiveFrom &&
               (!EffectiveUntil.HasValue || asOf <= EffectiveUntil.Value);
    }

    public bool AppliesToCoverage(CoverageType coverageType)
    {
        // If no coverage types specified, applies to all
        return _coverageTypes.Count == 0 || _coverageTypes.Contains(coverageType);
    }
}
