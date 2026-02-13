using MediatR;
using Vector.Domain.Common;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Command to add a rule to an underwriting guideline.
/// </summary>
public sealed record AddRuleCommand(
    Guid GuidelineId,
    string Name,
    string? Description,
    RuleType Type,
    RuleAction Action,
    int Priority,
    int? ScoreAdjustment,
    decimal? PricingModifier,
    string? Message) : IRequest<Result<Guid>>;
