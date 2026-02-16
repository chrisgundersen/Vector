using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;
using Vector.Domain.Submission.Enums;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for UpdatePairingCommand.
/// </summary>
public sealed class UpdatePairingCommandHandler(
    IProducerUnderwriterPairingRepository repository) : IRequestHandler<UpdatePairingCommand, Result>
{
    public async Task<Result> Handle(
        UpdatePairingCommand request,
        CancellationToken cancellationToken)
    {
        var pairing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (pairing is null)
        {
            return Result.Failure(new Error("Pairing.NotFound", $"Pairing with ID '{request.Id}' was not found."));
        }

        try
        {
            if (request.Priority > 0)
            {
                pairing.SetPriority(request.Priority);
            }

            pairing.SetEffectivePeriod(request.EffectiveFrom, request.EffectiveUntil);

            pairing.ClearCoverageTypes();
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

            await repository.UpdateAsync(pairing, cancellationToken);

            return Result.Success();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure(new Error("Pairing.InvalidValue", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error("Pairing.InvalidValue", ex.Message));
        }
    }
}
