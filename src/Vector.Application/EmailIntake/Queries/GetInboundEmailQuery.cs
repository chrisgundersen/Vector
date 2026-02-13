using MediatR;
using Vector.Application.EmailIntake.DTOs;

namespace Vector.Application.EmailIntake.Queries;

/// <summary>
/// Query to get an inbound email by ID.
/// </summary>
public sealed record GetInboundEmailQuery(Guid EmailId) : IRequest<InboundEmailDto?>;
