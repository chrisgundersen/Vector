using MediatR;
using Vector.Application.Routing.DTOs;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Queries;

/// <summary>
/// Handler for GetPairingsQuery.
/// </summary>
public sealed class GetPairingsQueryHandler(
    IProducerUnderwriterPairingRepository repository) : IRequestHandler<GetPairingsQuery, IReadOnlyList<PairingSummaryDto>>
{
    public async Task<IReadOnlyList<PairingSummaryDto>> Handle(
        GetPairingsQuery request,
        CancellationToken cancellationToken)
    {
        var pairings = await repository.GetActivePairingsAsync(cancellationToken);

        if (!request.ActiveOnly)
        {
            return pairings.Select(p => p.ToSummaryDto()).ToList();
        }

        // Filter to effective pairings only
        var now = DateTime.UtcNow;
        return pairings
            .Where(p => p.IsEffective(now))
            .Select(p => p.ToSummaryDto())
            .ToList();
    }
}
