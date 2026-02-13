using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Entities;
using Vector.Domain.UnderwritingGuidelines.ValueObjects;

namespace Vector.Application.UnderwritingGuidelines.DTOs;

public static class GuidelineMappingExtensions
{
    public static GuidelineSummaryDto ToSummaryDto(this UnderwritingGuideline guideline)
    {
        return new GuidelineSummaryDto(
            guideline.Id,
            guideline.Name,
            guideline.Description,
            guideline.Status.ToString(),
            guideline.EffectiveDate,
            guideline.ExpirationDate,
            guideline.Version,
            guideline.Rules.Count,
            guideline.CreatedAt);
    }

    public static GuidelineDto ToDto(this UnderwritingGuideline guideline)
    {
        return new GuidelineDto(
            guideline.Id,
            guideline.TenantId,
            guideline.Name,
            guideline.Description,
            guideline.Status.ToString(),
            guideline.EffectiveDate,
            guideline.ExpirationDate,
            guideline.Version,
            guideline.ApplicableCoverageTypes,
            guideline.ApplicableStates,
            guideline.ApplicableNAICSCodes,
            guideline.Rules.Select(r => r.ToDto()).ToList(),
            guideline.CreatedAt,
            guideline.CreatedBy,
            guideline.LastModifiedAt,
            guideline.LastModifiedBy);
    }

    public static RuleDto ToDto(this UnderwritingRule rule)
    {
        return new RuleDto(
            rule.Id,
            rule.Name,
            rule.Description,
            rule.Type.ToString(),
            rule.Action.ToString(),
            rule.Priority,
            rule.IsActive,
            rule.ScoreAdjustment,
            rule.PricingModifier,
            rule.Message,
            rule.Conditions.Select(c => c.ToDto()).ToList());
    }

    public static RuleConditionDto ToDto(this RuleCondition condition)
    {
        return new RuleConditionDto(
            condition.Field.ToString(),
            condition.Operator.ToString(),
            condition.Value,
            condition.SecondaryValue);
    }
}
