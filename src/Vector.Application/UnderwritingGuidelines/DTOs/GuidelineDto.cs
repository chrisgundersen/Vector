using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Application.UnderwritingGuidelines.DTOs;

/// <summary>
/// Summary DTO for underwriting guidelines list view.
/// </summary>
public sealed record GuidelineSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    int Version,
    int RuleCount,
    DateTime CreatedAt);

/// <summary>
/// Detailed DTO for underwriting guideline with rules.
/// </summary>
public sealed record GuidelineDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    string Status,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    int Version,
    string? ApplicableCoverageTypes,
    string? ApplicableStates,
    string? ApplicableNAICSCodes,
    IReadOnlyList<RuleDto> Rules,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? LastModifiedAt,
    string? LastModifiedBy);

/// <summary>
/// DTO for an underwriting rule.
/// </summary>
public sealed record RuleDto(
    Guid Id,
    string Name,
    string? Description,
    string Type,
    string Action,
    int Priority,
    bool IsActive,
    int? ScoreAdjustment,
    decimal? PricingModifier,
    string? Message,
    IReadOnlyList<RuleConditionDto> Conditions);

/// <summary>
/// DTO for a rule condition.
/// </summary>
public sealed record RuleConditionDto(
    string Field,
    string Operator,
    string? Value,
    string? SecondaryValue);
