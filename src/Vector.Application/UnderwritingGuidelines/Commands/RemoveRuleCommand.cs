using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Command to remove a rule from an underwriting guideline.
/// </summary>
public sealed record RemoveRuleCommand(
    Guid GuidelineId,
    Guid RuleId) : IRequest<Result>;
