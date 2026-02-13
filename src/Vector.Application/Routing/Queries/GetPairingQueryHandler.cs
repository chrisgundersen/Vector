using MediatR;
using Vector.Application.Routing.DTOs;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Queries;

/// <summary>
/// Handler for GetPairingQuery.
/// </summary>
public sealed class GetPairingQueryHandler(
    IProducerUnderwriterPairingRepository repository) : IRequestHandler<GetPairingQuery, PairingDto?>
{
    public async Task<PairingDto?> Handle(
        GetPairingQuery request,
        CancellationToken cancellationToken)
    {
        var pairing = await repository.GetByIdAsync(request.Id, cancellationToken);
        return pairing?.ToDto();
    }
}
