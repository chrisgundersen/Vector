using MediatR;
using Vector.Application.UnderwritingGuidelines.DTOs;

namespace Vector.Application.UnderwritingGuidelines.Queries;

/// <summary>
/// Query to get a guideline by ID with all rules.
/// </summary>
public sealed record GetGuidelineQuery(Guid Id) : IRequest<GuidelineDto?>;
