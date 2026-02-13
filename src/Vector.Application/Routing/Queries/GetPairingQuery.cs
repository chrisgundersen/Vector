using MediatR;
using Vector.Application.Routing.DTOs;

namespace Vector.Application.Routing.Queries;

/// <summary>
/// Query to get a producer-underwriter pairing by ID.
/// </summary>
public sealed record GetPairingQuery(Guid Id) : IRequest<PairingDto?>;
