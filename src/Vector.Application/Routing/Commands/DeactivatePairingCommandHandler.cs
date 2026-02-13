using MediatR;
using Vector.Domain.Common;
using Vector.Domain.Routing;

namespace Vector.Application.Routing.Commands;

/// <summary>
/// Handler for DeactivatePairingCommand.
/// </summary>
public sealed class DeactivatePairingCommandHandler(
    IProducerUnderwriterPairingRepository repository) : IRequestHandler<DeactivatePairingCommand, Result>
{
    public async Task<Result> Handle(
        DeactivatePairingCommand request,
        CancellationToken cancellationToken)
    {
        var pairing = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (pairing is null)
        {
            return Result.Failure(new Error("Pairing.NotFound", $"Pairing with ID '{request.Id}' was not found."));
        }

        pairing.Deactivate();

        await repository.UpdateAsync(pairing, cancellationToken);

        return Result.Success();
    }
}
