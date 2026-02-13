using MediatR;
using Vector.Application.UnderwritingGuidelines.DTOs;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Application.UnderwritingGuidelines.Queries;

/// <summary>
/// Query to get all guidelines for the current tenant with optional status filter.
/// </summary>
public sealed record GetGuidelinesQuery(
    GuidelineStatus? Status = null) : IRequest<IReadOnlyList<GuidelineSummaryDto>>;
