using MediatR;
using Vector.Domain.Common;

namespace Vector.Application.UnderwritingGuidelines.Commands;

/// <summary>
/// Command to create a new underwriting guideline.
/// </summary>
public sealed record CreateGuidelineCommand(
    string Name,
    string? Description,
    string? ApplicableCoverageTypes,
    string? ApplicableStates,
    string? ApplicableNAICSCodes,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate) : IRequest<Result<Guid>>;
