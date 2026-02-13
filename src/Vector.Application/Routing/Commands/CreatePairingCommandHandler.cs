using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;
using Vector.Domain.Routing.Entities;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for CreatePairingCommand.
/// </summary>
public sealed class CreatePairingCommandHandler(
    IProducerUnderwriterPairingRepository repository) : IRequestHandler<CreatePairingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreatePairingCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ProducerId == Guid.Empty)
        {
            return Result.Failure<Guid>(new Error("Pairing.InvalidProducerId", "Producer ID is required."));
        }

        if (request.UnderwriterId == Guid.Empty)
        {
            return Result.Failure<Guid>(new Error("Pairing.InvalidUnderwriterId", "Underwriter ID is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ProducerName))
        {
            return Result.Failure<Guid>(new Error("Pairing.ProducerNameRequired", "Producer name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.UnderwriterName))
        {
            return Result.Failure<Guid>(new Error("Pairing.UnderwriterNameRequired", "Underwriter name is required."));
        }

        var pairing = ProducerUnderwriterPairing.Create(
            request.ProducerId,
            request.ProducerName,
            request.UnderwriterId,
            request.UnderwriterName);

        try
        {
            if (request.Priority > 0)
            {
                pairing.SetPriority(request.Priority);
            }

            pairing.SetEffectivePeriod(request.EffectiveFrom, request.EffectiveUntil);

            if (request.CoverageTypes is { Count: > 0 })
            {
                foreach (var coverageTypeStr in request.CoverageTypes)
                {
                    if (Enum.TryParse<CoverageType>(coverageTypeStr, true, out var coverageType))
                    {
                        pairing.AddCoverageType(coverageType);
                    }
                }
            }

            await repository.AddAsync(pairing, cancellationToken);

            return Result.Success(pairing.Id);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure<Guid>(new Error("Pairing.InvalidValue", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(new Error("Pairing.InvalidValue", ex.Message));
        }
    }
}
