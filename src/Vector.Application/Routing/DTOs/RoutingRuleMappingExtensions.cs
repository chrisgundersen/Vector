using Vector.Domain.Routing.Aggregates;

namespace Vector.Application.Routing.DTOs;

public static class RoutingRuleMappingExtensions
{
    public static RoutingRuleSummaryDto ToSummaryDto(this RoutingRule rule) =>
        new(
            rule.Id,
            rule.Name,
            rule.Description,
            rule.Priority,
            rule.Status.ToString(),
            rule.Strategy.ToString(),
            rule.TargetUnderwriterName,
            rule.TargetTeamName,
            rule.Conditions.Count,
            rule.CreatedAt);

    public static RoutingRuleDto ToDto(this RoutingRule rule) =>
        new(
            rule.Id,
            rule.Name,
            rule.Description,
            rule.Priority,
            rule.Status.ToString(),
            rule.Strategy.ToString(),
            rule.TargetUnderwriterId,
            rule.TargetUnderwriterName,
            rule.TargetTeamId,
            rule.TargetTeamName,
            rule.Conditions.Select(c => c.ToDto()).ToList(),
            rule.CreatedAt,
            rule.CreatedBy,
            rule.LastModifiedAt);

    public static RoutingConditionDto ToDto(this Domain.Routing.ValueObjects.RoutingCondition condition) =>
        new(
            condition.Field.ToString(),
            condition.Operator.ToString(),
            condition.Value,
            condition.SecondaryValue);
}
