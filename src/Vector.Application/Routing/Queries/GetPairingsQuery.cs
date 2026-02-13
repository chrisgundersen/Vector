using MediatR;
using Vector.Application.Routing.DTOs;

namespace Vector.Application.Routing.Queries;

/// <summary>
/// Query to get all producer-underwriter pairings.
/// </summary>
public sealed record GetPairingsQuery(
    bool ActiveOnly = true) : IRequest<IReadOnlyList<PairingSummaryDto>>;
