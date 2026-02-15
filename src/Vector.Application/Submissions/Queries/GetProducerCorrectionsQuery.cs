using MediatR;
using Vector.Application.Submissions.DTOs;

namespace Vector.Application.Submissions.Queries;

/// <summary>
/// Query to get all data correction requests for a producer's submissions.
/// </summary>
public sealed record GetProducerCorrectionsQuery(
    Guid ProducerId,
    string? Status = null) : IRequest<IReadOnlyList<ProducerCorrectionDto>>;
