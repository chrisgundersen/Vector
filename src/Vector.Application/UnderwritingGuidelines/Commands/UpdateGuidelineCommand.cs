using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Command to update an existing underwriting guideline.
/// </summary>
public sealed record UpdateGuidelineCommand(
    Guid Id,
    string Name,
    string? Description,
    string? ApplicableCoverageTypes,
    string? ApplicableStates,
    string? ApplicableNAICSCodes,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate) : IRequest<Result>;
