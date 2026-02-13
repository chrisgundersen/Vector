namespace Vector.Application.Routing.DTOs;

/// <summary>
/// Summary DTO for routing rule listings.
/// </summary>
public sealed record RoutingRuleSummaryDto(
    Guid Id,
    string Name,
    string Description,
    int Priority,
    string Status,
    string Strategy,
    string? TargetUnderwriterName,
    string? TargetTeamName,
    int ConditionCount,
    DateTime CreatedAt);

/// <summary>
/// Detailed DTO for a single routing rule.
/// </summary>
public sealed record RoutingRuleDto(
    Guid Id,
    string Name,
    string Description,
    int Priority,
    string Status,
    string Strategy,
    Guid? TargetUnderwriterId,
    string? TargetUnderwriterName,
    Guid? TargetTeamId,
    string? TargetTeamName,
    IReadOnlyList<RoutingConditionDto> Conditions,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? LastModifiedAt);

/// <summary>
/// DTO for routing condition.
/// </summary>
public sealed record RoutingConditionDto(
    string Field,
    string Operator,
    string Value,
    string? SecondaryValue);
